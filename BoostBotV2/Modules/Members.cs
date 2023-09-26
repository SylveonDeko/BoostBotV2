using System.Diagnostics;
using System.Text.RegularExpressions;
using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using BoostBotV2.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BoostBotV2.Modules;

public partial class Members : BoostModuleBase
{
    private readonly DiscordAuthService _discordAuthService;
    private readonly DbService _db;
    private readonly Bot _bot;
    private readonly HttpClient _client;
    private readonly Credentials _credentials;

    private readonly Dictionary<ulong, int> _roleAllowances;

    public Members(DiscordAuthService discordAuthService, DbService db, Bot bot, HttpClient client, Credentials credentials)
    {
        _discordAuthService = discordAuthService;
        _db = db;
        _bot = bot;
        _client = client;
        _credentials = credentials;
        _roleAllowances = new Dictionary<ulong, int>
        {
            { _credentials.FreeRoleId, 4 },
            { _credentials.BronzeRoleId, 5 },
            { _credentials.SilverRoleId, 10 },
            { _credentials.GoldRoleId, 20 },
            { _credentials.PlatinumRoleId, 35 },
            { _credentials.PremiumRoleId, 50 }

        };
    }

    [Command("djoin")]
    [Summary("Joins a server with the allowed member count for the users highest role")]
    [Usage("djoin <guild id>")]
    [IsCommandGuild]
    [RateLimit(30)]
    [IsMembersChannel]
    public async Task JoinCommand(ulong guildId)
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var privatestock = false;
            if (!await GetAgreedRules(uow, _credentials))
                return;
            var registry = await uow.MemberFarmRegistry.FirstOrDefaultAsync(x => x.GuildId == guildId);

            if (uow.Blacklists.Select(x => x.BlacklistId).Contains(guildId))
            {
                await Context.Message.ReplyErrorAsync("That server ID is blacklisted.");
                return;
            }

            if (registry is not null)
            {
                if (registry.UserId != Context.Message.Author.Id && !_credentials.Owners.Contains(Context.User.Id))
                {
                    await Context.Message.ReplyErrorAsync("You are not allowed to add members to this server. This account is not the one that registered this server.");
                    return;
                }
            }
            else
            {
                await uow.MemberFarmRegistry.AddAsync(new MemberFarmRegistry
                {
                    GuildId = guildId,
                    UserId = Context.Message.Author.Id
                });
                await uow.SaveChangesAsync();
            }

            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=1&scope=bot%20applications.commands&guild_id={guildId}";
                var button = new ComponentBuilder()
                    .WithButton("Add To Server", style: ButtonStyle.Link, url: inviteUrl);
                var embed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($"You need to invite the bot to the server before using this command.")
                    .WithColor(Color.Red)
                    .Build();
                await Context.Message.ReplyAsync(embed: embed, components: button.Build());
                return;
            }

            var authorInGuild = await guild.GetUserAsync(Context.Message.Author.Id);
            if (authorInGuild == null)
            {
                await Context.Channel.SendErrorAsync("You must be in the specified server to use this command.");
                return;
            }

            if (guild.OwnerId != Context.User.Id)
            {
                if (!authorInGuild.JoinedAt.HasValue)
                {
                    await Context.Message.ReplyErrorAsync("I'm unable to detrermine when you joined the specified server. Have someone else try.");
                    return;
                }
                var timeSinceJoin = DateTime.UtcNow - authorInGuild.JoinedAt.Value.UtcDateTime;
                var timeSinceGuildCreation = DateTime.UtcNow - guild.CreatedAt.UtcDateTime;

                if (timeSinceJoin.TotalHours > timeSinceGuildCreation.TotalHours + 3)
                {
                    await Context.Message.ReplyErrorAsync("You joined the server too late after its creation, so you can't use this command for this server.");
                    return;
                }
            }
            
            var curUser = await guild.GetUserAsync(Context.Client.CurrentUser.Id);
            if (!curUser.GuildPermissions.Has(GuildPermission.CreateInstantInvite))
            {
                await Context.Message.ReplyErrorAsync("I don't have permission to add members in that server.");
                return;
            }

            var authorRoles = (Context.Message.Author as SocketGuildUser)?.Roles;
            var highestRole = authorRoles.MaxBy(role => _roleAllowances.TryGetValue(role.Id, out var value) ? value : 0);

            if (highestRole == null || !_roleAllowances.ContainsKey(highestRole.Id))
            {
                await Context.Message.ReplyErrorAsync("You don't have permission to add users to any guild.");
                return;
            }

            var (getAllowedAddCount, remainingTime) = await _discordAuthService.GetAllowedAddCount(Context.User.Id, highestRole.Id, guild.Id);
            var numTokens = _roleAllowances[highestRole.Id];

            if (getAllowedAddCount != null)
            {
                if (getAllowedAddCount <= 0)
                {
                    var promoteEb = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"You have reached your daily add limit. Try again at {TimestampTag.FromDateTime(remainingTime.Value)}." +
                                         $"\nYou can increase the limit by buying plans.")
                        .WithColor(Color.Red);

                    var button = new ComponentBuilder()
                        .WithButton("Store Link", url: _credentials.StoreLink, style: ButtonStyle.Link);

                    await Context.Message.ReplyAsync(embed: promoteEb.Build(), components: button.Build());
                    return;
                }

                numTokens = Math.Min(numTokens, getAllowedAddCount.Value);
            }

            var tokens = _bot.Tokens;
            var privateTokens = await _discordAuthService.GetPrivateStock(Context.User.Id);
            if (privateTokens != null && privateTokens.Any())
            {
                if (await PromptUserConfirmAsync("Would you like to use your private stock?", Context.User.Id))
                {
                    tokens = privateTokens;
                    privatestock = true;
                }
            }

            if (!tokens.Any())
            {
                await Context.Message.ReplyErrorAsync("# It seems we ran out of stock. Locking channel.");
                var channel = Context.Channel as ITextChannel;
                var current = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
                if (current.HasValue)
                {
                    var perms = current.Value.Modify(sendMessages: PermValue.Deny);
                    await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms);
                }
                await channel.SendMessageAsync("# Do not ask when we will restock. Do not ask why this channel is locked. We will tell you when we restock. Thinking with your ass and not looking with your eyes at this message will get you a timeout.");
                return;
            }

            var successCount = 0;
            var eb = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"{_credentials.LoadingEmote} Adding {numTokens} members to the guild '{guild}' for the role '{highestRole.Name}'");
            var message = await Context.Message.ReplyAsync(embed: eb.Build());

            var usedTokens = new HashSet<string>();
            var guildUsedTokens = uow.GuildsAdded.Where(x => x.GuildId == guild.Id).Select(x => x.Token).ToHashSet();
            var availableTokens = new HashSet<string>(tokens.Except(guildUsedTokens));
            var sw = new Stopwatch();
            sw.Start();
            foreach (var token in availableTokens)
            {
                if (successCount >= numTokens)
                    break;
                if (!await _discordAuthService.Authorizer(token, guild.Id.ToString()))
                {
                    if (privatestock)
                    {
                        await _discordAuthService.RemovePrivateToken(token, Context.User.Id);
                    }
                    continue;
                }
                successCount += 1;
                usedTokens.Add(token);
            }
            sw.Stop();

            if (successCount > 0)
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"{_credentials.SuccessEmote} Added members to the guild '{guild}' for the role '{highestRole.Name}' ({successCount}/{numTokens} tokens used).")
                    .WithFooter($"Took {sw.Elapsed:g} to add {successCount} members to {guild}")
                    .WithColor(Color.Green)
                    .Build();
                await message.ModifyAsync(msg => msg.Embed = embed);
                await uow.GuildsAdded.AddRangeAsync(usedTokens.Select(x => new GuildsAdded { GuildId = guild.Id, Token = x }));
                await uow.SaveChangesAsync();
            }
            else
            {
                await Context.Message.ReplyErrorAsync("Failed to add members. No stock left to add to your server.");
            }
        }
        catch (Exception e)
        {
            Log.Error("Error adding members: {Error}", e);
        }
    }


    [Command("djoinonline")]
    [Summary("Joins a server with the allowed member count for the users highest role (online mode)")]
    [Usage("djoinonline <guild id>")]
    [IsCommandGuild]
    [RateLimit(30)]
    [IsMembersChannel]
    [IsPremium]
    public async Task OnlineJoinCommand(ulong guildId)
    {
        try
        {
            await using var uow = _db.GetDbContext();
            if (!await GetAgreedRules(uow, _credentials))
                return;
            var registry = await uow.MemberFarmRegistry.FirstOrDefaultAsync(x => x.GuildId == guildId);

            if (uow.Blacklists.Select(x => x.BlacklistId).Contains(guildId))
            {
                await Context.Message.ReplyErrorAsync("That server ID is blacklisted.");
                return;
            }

            if (registry is not null)
            {
                if (registry.UserId != Context.Message.Author.Id)
                {
                    await Context.Message.ReplyErrorAsync("You are not allowed to add members to this server. This account is not the one that registered this server.");
                    return;
                }
            }
            else
            {
                await uow.MemberFarmRegistry.AddAsync(new MemberFarmRegistry
                {
                    GuildId = guildId,
                    UserId = Context.Message.Author.Id
                });
                await uow.SaveChangesAsync();
            }

            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=1&scope=bot%20applications.commands&guild_id={guildId}";
                var button = new ComponentBuilder()
                    .WithButton("Add To Server", style: ButtonStyle.Link, url: inviteUrl);
                var embed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($"You need to invite the bot to the server before using this command.")
                    .WithColor(Color.Red)
                    .Build();
                await Context.Message.ReplyAsync(embed: embed, components: button.Build());
                return;
            }

            var authorInGuild = await guild.GetUserAsync(Context.Message.Author.Id);
            if (authorInGuild == null)
            {
                await Context.Channel.SendErrorAsync("You must be in the specified server to use this command.");
                return;
            }

            if (guild.OwnerId != Context.User.Id)
            {
                if (!authorInGuild.JoinedAt.HasValue)
                {
                    await Context.Message.ReplyErrorAsync("I'm unable to detrermine when you joined the specified server. Have someone else try.");
                    return;
                }
                var timeSinceJoin = DateTime.UtcNow - authorInGuild.JoinedAt.Value.UtcDateTime;
                var timeSinceGuildCreation = DateTime.UtcNow - guild.CreatedAt.UtcDateTime;

                if (timeSinceJoin.TotalHours > timeSinceGuildCreation.TotalHours + 3)
                {
                    await Context.Message.ReplyErrorAsync("You joined the server too late after its creation, so you can't use this command for this server.");
                    return;
                }
            }
            var curUser = await guild.GetUserAsync(Context.Client.CurrentUser.Id);
            if (curUser is null)
            {
                await Context.Channel.SendErrorAsync("You must be in the specified server to use this command.");
                return;
            }
            if (!curUser.GuildPermissions.Has(GuildPermission.CreateInstantInvite))
            {
                await Context.Message.ReplyErrorAsync("I don't have permission to add members in that server.");
                return;
            }

            var authorRoles = (Context.Message.Author as SocketGuildUser)?.Roles;
           var highestRole = authorRoles.MaxBy(role => _roleAllowances.TryGetValue(role.Id, out var value) ? value : 0);

            if (highestRole == null || !_roleAllowances.ContainsKey(highestRole.Id))
            {
                await Context.Message.ReplyErrorAsync("You don't have permission to add users to any guild.");
                return;
            }

            var (getAllowedAddCount, remainingTime) = await _discordAuthService.GetAllowedAddCount(Context.User.Id, highestRole.Id, guild.Id);
            var numTokens = _roleAllowances[highestRole.Id];

            if (getAllowedAddCount != null)
            {
                if (getAllowedAddCount <= 0)
                {
                    var promoteEb = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"You have reached your daily add limit. Try again at {TimestampTag.FromDateTime(remainingTime.Value)}." +
                                         $"\nYou can increase the limit by buying plans.")
                        .WithColor(Color.Red);

                    var button = new ComponentBuilder()
                        .WithButton("Store Link", url: _credentials.StoreLink, style: ButtonStyle.Link);

                    await Context.Message.ReplyAsync(embed: promoteEb.Build(), components: button.Build());
                    return;
                }

                numTokens = Math.Min(numTokens, getAllowedAddCount.Value);
            }

            var tokens = _bot.OnlineTokens;
            var privateTokens = await _discordAuthService.GetPrivateStock(Context.User.Id, true);
            var privateStock = false;
            if (privateTokens != null && privateTokens.Any())
            {
                if (await PromptUserConfirmAsync("Would you like to use your private stock?", Context.User.Id))
                {
                    tokens = privateTokens;
                    privateStock = true;
                }
            }

            if (!tokens.Any())
            {
                await Context.Message.ReplyErrorAsync("No Stock.");
                return;
            }

            var successCount = 0;
            var failedCount = 0;
            var eb = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"{_credentials.LoadingEmote} Adding {numTokens} online members to the guild '{guild}' for the role '{highestRole.Name}'");
            var message = await Context.Message.ReplyAsync(embed: eb.Build());

            var usedTokens = new HashSet<string>();
            var guildUsedTokens = uow.GuildsAdded.Where(x => x.GuildId == guild.Id).Select(x => x.Token).ToHashSet();
            var availableTokens = new HashSet<string>(tokens.Except(guildUsedTokens));
            var sw = new Stopwatch();
            sw.Start();
            foreach (var token in availableTokens)
            {
                if (successCount >= numTokens)
                    break;
                if (!await _discordAuthService.Authorizer(token, guild.Id.ToString()))
                {
                    if (privateStock)
                    {
                        await _discordAuthService.RemovePrivateToken(token, Context.User.Id);
                    }
                    continue;
                }
                successCount += 1;
                usedTokens.Add(token);
            }
            sw.Stop();

            if (successCount > 0)
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"{_credentials.SuccessEmote} Added online members to the guild '{guild}' for the role '{highestRole.Name}' ({successCount}/{numTokens} members used).")
                    .WithFooter($"Took {sw.Elapsed:g} to add {successCount} members. Removed {failedCount} non working members.")
                    .WithColor(Color.Green)
                    .Build();
                await message.ModifyAsync(msg => msg.Embed = embed);
                await uow.GuildsAdded.AddRangeAsync(usedTokens.Select(x => new GuildsAdded { GuildId = guild.Id, Token = x }));
                await uow.SaveChangesAsync();
            }
            else
            {
                await Context.Message.ReplyErrorAsync("Failed to add members. No stock left to add to your server.");
            }
        }
        catch (Exception e)
        {
            Log.Error("Error adding members: {Error}", e);
        }
    }


    [Command("stock")]
    [Summary("Shows the amount of members left in stock")]
    [Usage("stock")]
    [IsCommandGuild]
    public async Task Stock()
    {
        await using var uow = _db.GetDbContext();
        var privateOfflineStock = await _discordAuthService.GetPrivateStock(Context.User.Id);
        var privateOnlineStock = await _discordAuthService.GetPrivateStock(Context.User.Id, true);
        var keepOnlineStock = await uow.KeepOnline.CountAsync(x => x.UserId == Context.User.Id);
        var nitroStock = _discordAuthService.GetBoostTokens(Context.User.Id);
        var file = await File.ReadAllLinesAsync("tokens.txt");
        var onlineFile = await File.ReadAllLinesAsync("onlinetokens.txt");
        var embed = new EmbedBuilder()
            .WithColor(Color.Green)
            .AddField("Regular Stock", $"**Regular Members:** {file.Length}\n**Online Members:** {onlineFile.Length}")
            .AddField("Private Stock", $"**Regular Members:** {privateOfflineStock?.Count ?? 0}\n**Online Members:** {privateOnlineStock?.Count ?? 0}\n**Boost:** {nitroStock.Count}")
            .AddField("Keep Online", $"**Members:** {keepOnlineStock}")
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("addedmembers")]
    [Summary("Shows the amount of members added to a guild")]
    [Usage("addedmembers <guild id>")]
    [IsCommandGuild]
    public async Task AddedMembers(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        if (!await GetAgreedRules(uow, _credentials))
            return;
        var guild = await Context.Client.GetGuildAsync(guildId);
        if (guild == null)
        {
            await ReplyAsync("Guild not found.");
            return;
        }

        var addedMembers = uow.GuildsAdded.Count(x => x.GuildId == guild.Id);
        var embed = new EmbedBuilder()
            .WithColor(Color.Purple)
            .WithDescription($"**Added Members:** {addedMembers}")
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("addstock")]
    [Summary("Adds members to the stock")]
    [Usage("addstock <online/offline> <attachment>")]
    [OwnerOnlyHelp]
    [IsOwner]
    public async Task AddStock(StockEnum stockEnum = StockEnum.Offline)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            await Context.Channel.SendErrorAsync("No attachment found.");
            return;
        }

        if (attachment.Filename.Split('.').LastOrDefault() != "txt")
        {
            await Context.Channel.SendErrorAsync("Invalid file type.");
            return;
        }

        var file = await _client.GetStringAsync(attachment.Url);
        var fileSplit = file.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var x in fileSplit)
        {
            if (MyRegex().IsMatch(x)) continue;
            Log.Information(x);
            await Context.Channel.SendErrorAsync("One or mode invalid members found.");
            return;
        }

        switch (stockEnum)
        {
            case StockEnum.Online:

                await File.AppendAllLinesAsync("onlinetokens.txt", fileSplit);
                foreach (var i in fileSplit)
                {
                    _bot.OnlineTokens.Add(i);
                }

                await ReplyAsync($"Added {fileSplit.Length} online members to the stock.");
                break;
            case StockEnum.Offline:
                await File.AppendAllLinesAsync("tokens.txt", fileSplit);
                foreach (var i in fileSplit)
                {
                    _bot.Tokens.Add(i);
                }

                await ReplyAsync($"Added {fileSplit.Length} offline members to the stock.");
                break;
        }
    }

    [Command("addprivatestock")]
    [Summary("Adds members to your private stock")]
    [Usage("addprivatestock <online/offline> <attachment>")]
    [RateLimit(90)]
    public async Task AddPrivateStock(StockEnum stockEnum = StockEnum.Offline)
    {
        await using var uow = _db.GetDbContext();
        if (!await GetAgreedRules(uow, _credentials))
            return;
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            await Context.Channel.SendErrorAsync("No attachment found.");
            return;
        }

        if (attachment.Filename.Split('.').LastOrDefault() != "txt")
        {
            await Context.Channel.SendErrorAsync("Invalid file type.");
            return;
        }

        var file = await _client.GetStringAsync(attachment.Url);
        var fileSplit = file.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var x in fileSplit)
        {
            if (MyRegex().IsMatch(x)) continue;
            Log.Information(x);
            await Context.Channel.SendErrorAsync("One or mode invalid members found.");
            return;
        }

        switch (stockEnum)
        {
            case StockEnum.Online:

                await File.AppendAllLinesAsync("onlinetokens.txt", fileSplit);
                await _discordAuthService.AddMultiplePrivateStock(Context.User.Id, fileSplit, true);
                await ReplyAsync($"Added {fileSplit.Length} online members to your private stock.");
                break;
            case StockEnum.Offline:
                await File.AppendAllLinesAsync("tokens.txt", fileSplit);
                await _discordAuthService.AddMultiplePrivateStock(Context.User.Id, fileSplit);
                await ReplyAsync($"Added {fileSplit.Length} offline members to your private stock.");
                break;
        }
    }


    [Command("djoinmass")]
    [Summary("Joins a server with the allowed member count for the users highest role, en masse")]
    [Usage("djoinmass stocktype <guild id> count")]
    [IsCommandGuild]
    [RateLimit(30)]
    [IsMembersChannel]
    [IsPremium]
    public async Task MassJoinCommand(StockEnum type, ulong guildId, int count)
    {
        try
        {
            await using var uow = _db.GetDbContext();
            if (!await GetAgreedRules(uow, _credentials))
                return;
            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=1&scope=bot%20applications.commands&guild_id={guildId}";
                var button = new ComponentBuilder()
                    .WithButton("Add To Server", style: ButtonStyle.Link, url: inviteUrl);
                var embed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($"You need to invite the bot to the server before using this command.")
                    .WithColor(Color.Red)
                    .Build();
                await Context.Message.ReplyAsync(embed: embed, components: button.Build());
                return;
            }

            var curUser = await guild.GetUserAsync(Context.Client.CurrentUser.Id);
            if (!curUser.GuildPermissions.Has(GuildPermission.CreateInstantInvite))
            {
                await Context.Message.ReplyErrorAsync("I don't have permission to add members in that server.");
                return;
            }

            var authorRoles = (Context.Message.Author as SocketGuildUser)?.Roles;
            var highestRole = authorRoles.MaxBy(role => _roleAllowances.TryGetValue(role.Id, out var value) ? value : 0);

            if (highestRole == null || !_roleAllowances.ContainsKey(highestRole.Id))
            {
                await Context.Message.ReplyErrorAsync("You don't have permission to add users to any guild.");
                return;
            }

            var (getAllowedAddCount, remainingTime) = await _discordAuthService.GetAllowedAddCount(Context.User.Id, highestRole.Id, guild.Id);
            if (getAllowedAddCount != null && count > getAllowedAddCount.Value)
            {
                if (getAllowedAddCount <= 0)
                {
                    var promoteEb = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"You have reached your daily add limit. Try again at {TimestampTag.FromDateTime(remainingTime.Value)}." +
                                         $"\nYou can increase the limit by buying plans.")
                        .WithColor(Color.Red);

                    var button = new ComponentBuilder()
                        .WithButton("Store Link", url: _credentials.StoreLink, style: ButtonStyle.Link);

                    await Context.Message.ReplyAsync(embed: promoteEb.Build(), components: button.Build());
                    return;
                }

                else
                {
                    var promoteEb = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"You are adding more than your limit allows." +
                                         $"\nYou can increase the limit by buying plans.")
                        .WithColor(Color.Red);

                    var button = new ComponentBuilder()
                        .WithButton("Store Link", url: _credentials.StoreLink, style: ButtonStyle.Link);

                    await Context.Message.ReplyAsync(embed: promoteEb.Build(), components: button.Build());
                }
            }

            HashSet<string> tokens;
            switch (type)
            {
                case StockEnum.Online:
                    var stock = await _discordAuthService.GetPrivateStock(Context.User.Id, true);
                    if (stock is null || !stock.Any())
                    {
                        await Context.Message.ReplyErrorAsync("You don't have any members in your private offline stock.");
                        return;
                    }

                    tokens = stock;
                    break;

                case StockEnum.Offline:
                    var stockonline = await _discordAuthService.GetPrivateStock(Context.User.Id);
                    if (stockonline is null || !stockonline.Any())
                    {
                        await Context.Message.ReplyErrorAsync("You don't have any members in your private stock.");
                        return;
                    }

                    tokens = stockonline;
                    break;

                default:
                    await Context.Channel.SendErrorAsync("How the fuck did you get here?");
                    return;
            }

            if (count > tokens.Count)
            {
                await Context.Channel.SendErrorAsync("Amount of members to add is more than the amount in stock.");
                return;
            }

            if (!tokens.Any())
            {
                await Context.Message.ReplyErrorAsync("No Stock.");
                return;
            }

            var successCount = 0;
            var failedCount = 0;
            var eb = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"{_credentials.LoadingEmote} Adding {count} members to the guild '{guild}' for the role '{highestRole.Name}'");
            var message = await Context.Message.ReplyAsync(embed: eb.Build());

            var usedTokens = new HashSet<string>();
            var guildUsedTokens = uow.GuildsAdded.Where(x => x.GuildId == guild.Id).Select(x => x.Token).ToHashSet();

            // Create a temporary HashSet that excludes tokens already used in the guild
            var availableTokens = new HashSet<string>(tokens.Except(guildUsedTokens));

            if (availableTokens.Count < count && !availableTokens.Any())
            {
                await Context.Message.ReplyErrorAsync($"Amount of tokens left is less than the amount of members you want to add ({availableTokens.Count}). Adding anyway.");
            }

            var sw = new Stopwatch();
            sw.Start();

            foreach (var token in availableTokens)
            {
                if (successCount >= count)
                    break;
                if (!await _discordAuthService.Authorizer(token, guild.Id.ToString()))
                {
                    await _discordAuthService.RemovePrivateToken(token, Context.User.Id, type == StockEnum.Online);
                    continue;
                }
                successCount += 1;
                usedTokens.Add(token);
            }
            sw.Stop();

            if (successCount > 0)
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"{_credentials.SuccessEmote} Added the members to the guild '{guild}' for the role '{highestRole.Name}' ({successCount}/{count} members used).")
                    .WithFooter($"Took {sw.Elapsed:g} to add {successCount} members. Removed {failedCount} non working members.")
                    .WithColor(Color.Green)
                    .Build();
                await message.ModifyAsync(msg => msg.Embed = embed);
                await uow.GuildsAdded.AddRangeAsync(usedTokens.Select(x => new GuildsAdded { GuildId = guild.Id, Token = x }));
                await uow.SaveChangesAsync();
            }
            else
            {
                await Context.Message.ReplyErrorAsync("Failed to add members. No stock left to add to your server.");
            }
        }
        catch (Exception e)
        {
            Log.Error("Error adding members: {Error}", e);
        }
    }

    [Command("addkeeponline")]
    [Summary("Adds members to your keep online list. The bot keeps them online while its on.")]
    [Usage("addkeeponline <attachment>")]
    [IsPremium]
    [IsCommandGuild]
    [RateLimit(200)]
    public async Task AddKeepOnline()
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            await Context.Channel.SendErrorAsync("No attachment found.");
            return;
        }
        
        if (attachment.Filename.Split('.').LastOrDefault() != "txt")
        {
            await Context.Channel.SendErrorAsync("Invalid file type.");
            return;
        }
        
        var file = await _client.GetStringAsync(attachment.Url);
        var fileSplit = file.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var x in fileSplit)
        {
            if (MyRegex().IsMatch(x)) continue;
            Log.Information(x);
            await Context.Channel.SendErrorAsync("One or mode invalid members found.");
            return;
        }
        
        await using var uow = _db.GetDbContext();
        if (!await GetAgreedRules(uow, _credentials))
            return;
        
        var toAdd = fileSplit.ToHashSet();
        var current = uow.KeepOnline.Where(x => x.UserId == Context.User.Id).Select(x => x.Token).ToHashSet();
        var fixedToAdd = toAdd.Except(current);
        if (!fixedToAdd.Any())
        {
            await Context.Channel.SendErrorAsync("No new tokens to add.");
            return;
        }
        uow.KeepOnline.AddRange(fixedToAdd.Select(x => new KeepOnline {UserId = Context.User.Id, Token = x}));
        await uow.SaveChangesAsync();
        await Context.Channel.SendConfirmAsync($"Added {fixedToAdd.Count()} tokens to your keep online list.");

    }

    public enum StockEnum
    {
        Online,
        Offline
    }

    [GeneratedRegex(@"^[a-zA-Z0-9+/=]+\.[a-zA-Z0-9_-]{2,7}\.[a-zA-Z0-9_-]{5,40}$")]
    private static partial Regex MyRegex();
}