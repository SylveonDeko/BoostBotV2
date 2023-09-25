using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Common.AutoCompleters;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace BoostBotV2.SlashModule;

[Group("owner", "Owner Commands.")]
public class OwnerOnly : BoostInteractionModuleBase
{
    private readonly Credentials _creds;
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly InteractiveService _interactive;

    public OwnerOnly(Credentials creds, DbService db, DiscordSocketClient client, InteractiveService interactive)
    {
        _creds = creds;
        _db = db;
        _client = client;
        _interactive = interactive;
    }
    
    [SlashCommand("leaveinactive", "Leave inactive servers")]
    [IsOwner]
    public async Task LeaveInactive()
    {
        await DeferAsync();
        HashSet<SocketGuild> toLeave = new();
        await using var uow = _db.GetDbContext();
        var guilds = uow.GuildsAdded.Where(x => x.DateAdded > DateTime.UtcNow.AddDays(-2)).ToHashSet();
        var actualGuilds = _client.Guilds;
        foreach (var i in actualGuilds)
        {
            if (i.Id == _creds.CommandGuild) continue;
            if (!guilds.Select(x => x.GuildId).Contains(i.Id))
                toLeave.Add(i);
        }

        foreach (var toAdd in guilds.Select(i => _client.GetGuild(i.GuildId)).Where(toAdd => toAdd is not null))
        {
            toLeave.Add(toAdd);
        }
        
        if (toLeave.Count == 0)
        {
            await Context.Interaction.SendErrorAsync("No inactive guilds found.");
            return;
        }
        
        if (toLeave.Contains(Context.Guild as SocketGuild))
            toLeave.Remove(Context.Guild as SocketGuild);

        if (await PromptUserConfirmAsync($"Are you sure you want to leave {toLeave.Count} guilds?", Context.User.Id, true))
        {
            await Context.Interaction.SendErrorAsync("Leaving guilds...");
            foreach (var guild in toLeave)
            {
                await guild.LeaveAsync();
            }

            await Context.Interaction.SendErrorAsync("Done.");
        }

    }

    [SlashCommand("leave", "Leave a server")]
    [IsOwner]
    public async Task Leave([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var id))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
        var guild = _client.GetGuild(id);
        if (guild is null)
        {
            await Context.Interaction.SendErrorAsync("Guild not found.");
            return;
        }

        if (await PromptUserConfirmAsync($"Are you sure you want to leave {guild.Name}?", Context.User.Id, true))
        {
            await guild.LeaveAsync();
            await Context.Interaction.SendErrorAsync("Done.");
        }
    }
    
    [SlashCommand("listservers", "Lists all servers the bot is in")]
    [IsOwner]
    public async Task ListServers()
    {
        var guilds = _client.Guilds;
        var paginator = new LazyPaginatorBuilder()
            .AddUser(Context.User)
            .WithPageFactory(PageFactory)
            .WithFooter(PaginatorFooter.PageNumber | PaginatorFooter.Users)
            .WithMaxPageIndex(guilds.Count-1 / 5)
            .WithDefaultEmotes()
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .Build();

        await _interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(60)).ConfigureAwait(false);

        async Task<PageBuilder> PageFactory(int page)
        {
            await Task.CompletedTask;

            var pageb = new PageBuilder()
                .WithColor(Color.Green)
                .WithAuthor($"Servers - {guilds.Count}");
            
            var guildsOnPage = guilds.Skip(page * 5).Take(5).ToList();
            foreach (var guild in guildsOnPage)
            {
                pageb.AddField(guild.Name, $"ID: {guild.Id}\nMembers: {guild.MemberCount}\nOwner: {guild.Owner.Username}#{guild.Owner.Discriminator}\nOwnerId: {guild.Owner.Id}");
            }

            return pageb;
        }
    }

    [SlashCommand("setlogchannel", "Sets the log channel.")]
    [IsOwner]
    public async Task SetLogChannel(ITextChannel channel)
    {
        await DeferAsync();
        _creds.CommandLogChannel = channel.Id;
        SerializeYml.Serialize(_creds);
        await Context.Interaction.SendConfirmAsync("Log channel set.");
    }
    
    [SlashCommand("setcommandguild", "Sets the command guild.")]
    [IsOwner]
    public async Task SetCommandGuild([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
        _creds.CommandGuild = guildId;
        SerializeYml.Serialize(_creds);
        await Context.Interaction.SendConfirmAsync("Command guild set.");
    }
    
    [SlashCommand("setfarmchannel", "Sets the member farm channel.")]
    [IsOwner]
    public async Task SetFarmChannel(ITextChannel channel)
    {
        await DeferAsync();
        _creds.FarmChannel = channel.Id;
        SerializeYml.Serialize(_creds);
        await Context.Interaction.SendConfirmAsync("Farm channel set.");
    }

    [SlashCommand("resetrules", "Resets rules selection for a user")]
    [IsOwner]
    public async Task ResetRules(IUser user)
    {
        await using var uow = _db.GetDbContext();
        var exists = uow.RulesAgreed.FirstOrDefault(x => x.UserId == user.Id);
        if (exists is not null)
        {
            if (await PromptUserConfirmAsync("Are you sure you want to reset rules for this user?", Context.User.Id))
            {
                uow.RulesAgreed.Remove(exists);
                await uow.SaveChangesAsync();
            }
        }
    }
    
    [SlashCommand("resetdjoin", "Resets the registed user for a guild")]
    [IsOwner]
    public async Task ResetDJoin([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await using var uow = _db.GetDbContext();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
        var exists = uow.MemberFarmRegistry.FirstOrDefault(x => x.GuildId == guildId);
        if (exists is not null)
        {
            if (await PromptUserConfirmAsync("Are you sure you want to reset this guild?", Context.User.Id))
            {
                uow.MemberFarmRegistry.Remove(exists);
                await uow.SaveChangesAsync();
            }
        }
        else
            await Context.Interaction.SendErrorAsync("Guild not found.");
    }
    
    [SlashCommand("getregistered", "Gets the registered user for a guild")]
    [IsOwner]
    public async Task GetRegistered([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
        await using var uow = _db.GetDbContext();
        var exists = uow.MemberFarmRegistry.FirstOrDefault(x => x.GuildId == guildId);
        if (exists is not null)
        {
            var client = Context.Client as DiscordSocketClient;
            var fetched = await client.Rest.GetUserAsync(exists.UserId);

            if (fetched is not null)
            {
                var eb = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithThumbnailUrl(fetched.GetAvatarUrl())
                    .WithDescription($"Name: {fetched}\nID: {fetched.Id}\nRegistered On: {TimestampTag.FromDateTime(exists.DateAdded)}");
                await Context.Interaction.FollowupAsync(embed: eb.Build());
            }
            
            else
                await Context.Interaction.SendErrorAsync("User not found");
        }
        else
        {
            await Context.Interaction.SendErrorAsync("Guild not registered.");
        }
    }
    
    [SlashCommand("settieredrole", "Sets the tiered role ID")]
    [IsOwner]
    public async Task SetTieredRole(Tier tier, IRole role)
    {
        await DeferAsync();
        switch (tier)
        {
            case Tier.Free:
                if (_creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.FreeRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Free role set {role.Mention}");
                break;
            case Tier.Bronze:
                if (_creds.FreeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.BronzeRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Bronze role set {role.Mention}");
                break;
            case Tier.Silver:
                if (_creds.BronzeRoleId == role.Id || _creds.FreeRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.SilverRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Silver role set {role.Mention}");
                break;
            case Tier.Gold:
                if (_creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.FreeRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.GoldRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Gold role set {role.Mention}");
                break;
            case Tier.Platinum:
                if (_creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.FreeRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.PlatinumRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Platinum role set {role.Mention}");
                break;
            case Tier.Premium:
                if (_creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.FreeRoleId == role.Id)
                {
                    await Context.Interaction.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.PremiumRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Premium role set {role.Mention}");
                break;
        }
    }
    
    [SlashCommand("setemotes", "Sets the emotes for the bot")]
    [IsOwner]
    public async Task SetEmotes(EmoteType type, string emote2)
    {
        await DeferAsync();
        switch (type)
        {
            case EmoteType.Success:
                if (emote2.ToIEmote() is null)
                {
                    await Context.Interaction.SendErrorAsync("Emote not found.");
                    return;
                }
                _creds.SuccessEmote = emote2;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Success emote set to {emote2}");
                break;
            case EmoteType.Loading:
                if (emote2.ToIEmote() is null)
                {
                    await Context.Interaction.SendErrorAsync("Emote not found.");
                    return;
                }
                _creds.LoadingEmote = emote2;
                SerializeYml.Serialize(_creds);
                await Context.Interaction.SendConfirmAsync($"Loading emote set to {emote2}");
                break;
        }
    }

    public enum EmoteType
    {
        Loading,
        Success
    }

    public enum Tier
    {
        Free,
        Bronze,
        Silver,
        Gold,
        Platinum,
        Premium
    }
}