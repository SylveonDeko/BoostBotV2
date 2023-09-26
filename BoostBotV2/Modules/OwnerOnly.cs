using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BoostBotV2.Modules;

public class OwnerOnly : BoostModuleBase
{
    private readonly Credentials _creds;
    private readonly DbService _db;
    private readonly Bot _bot;

    public OwnerOnly(Credentials creds, DbService db, Bot bot)
    {
        _creds = creds;
        _db = db;
        _bot = bot;
    }
    
    [Command("sudo")]
    [Summary("Runs a command as another user.")]
    [Usage("sudo <user> <command anmd args>")]
    [IsOwner]
    public async Task Sudo(IUser user, [Remainder] string command)
    {
        try
        {
            await Context.Channel.SendConfirmAsync("Running command...");
            var msg = new AIOMessage
            {
                Author = user,
                Content = command,
                Channel = Context.Channel,
                Source = MessageSource.User
            };
            await _bot.HandleCommandAsync(msg);
        }
        catch (Exception ex)
        {
            await Context.Channel.SendErrorAsync($"Error running command.\n{ex}");
        }
    }

    [Command("setlogchannel")]
    [Summary("Sets the log channel.")]
    [Usage("setlogchannel <channel>")]
    [IsOwner]
    public async Task SetLogChannel(ITextChannel channel)
    {
        _creds.CommandLogChannel = channel.Id;
        SerializeYml.Serialize(_creds);
        await ReplyAsync("Log channel set.");
    }
    
    [Command("setcommandguild")]
    [Summary("Sets the command guild.")]
    [Usage("setcommandguild <guild>")]
    [IsOwner]
    public async Task SetCommandGuild(ulong guildId)
    {
        _creds.CommandGuild = guildId;
        SerializeYml.Serialize(_creds);
        await ReplyAsync("Command guild set.");
    }
    
    [Command("setfarmchannel")]
    [Summary("Sets the farm channel.")]
    [Usage("setfarmchannel <channel>")]
    [IsOwner]
    public async Task SetFarmChannel(ITextChannel channel)
    {
        _creds.FarmChannel = channel.Id;
        SerializeYml.Serialize(_creds);
        await ReplyAsync("Farm channel set.");
    }

    [Command("resetrules")]
    [Summary("Resets rules selection for a user")]
    [Usage("resetrules userid")]
    [IsOwner]
    public async Task ResetRules(ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var exists = uow.RulesAgreed.FirstOrDefault(x => x.UserId == userId);
        if (exists is not null)
        {
            if (await PromptUserConfirmAsync("Are you sure you want to reset rules for this user?", Context.User.Id))
            {
                uow.RulesAgreed.Remove(exists);
                await uow.SaveChangesAsync();
            }
        }
    }
    
    [Command("resetdjoin")]
    [Summary("Resets the registed user for a guild")]
    [Usage("resetdjoin guildid")]
    [IsOwner]
    public async Task ResetDJoin(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
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
            await Context.Channel.SendErrorAsync("Guild not found.");
    }
    
    [Command("getregistered")]
    [Summary("Gets the registered user for a guild")]
    [Usage("getregistered guildid")]
    [IsOwner]
    public async Task GetRegistered(ulong guildId)
    {
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
                await ReplyAsync(embed: eb.Build());
            }
            
            else
                await Context.Channel.SendErrorAsync("User not found");
        }
        else
        {
            await Context.Channel.SendErrorAsync("Guild not registered.");
        }
    }
    
    [Command("settieredrole")]
    [Summary("Sets the tiered role ID")]
    [Usage("settieredrole <free|bronze|silver|gold|platinum|premium> @role")]
    [IsOwner]
    public async Task SetTieredRole(Tier tier, [Remainder] IRole role)
    {
        switch (tier)
        {
            case Tier.Free:
                if (_creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.FreeRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Free role set {role.Mention}");
                break;
            case Tier.Bronze:
                if (_creds.FreeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.BronzeRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Bronze role set {role.Mention}");
                break;
            case Tier.Silver:
                if (_creds.FreeRoleId == role.Id || _creds.BronzeRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.SilverRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Silver role set {role.Mention}");
                break;
            case Tier.Gold:
                _creds.GoldRoleId = role.Id;
                if (_creds.FreeRoleId == role.Id || _creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.PlatinumRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Gold role set {role.Mention}");
                break;
            case Tier.Platinum:
                if (_creds.FreeRoleId == role.Id || _creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PremiumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.PlatinumRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Platinum role set {role.Mention}");
                break;
            case Tier.Premium:
                if (_creds.FreeRoleId == role.Id || _creds.BronzeRoleId == role.Id || _creds.SilverRoleId == role.Id || _creds.GoldRoleId == role.Id || _creds.PlatinumRoleId == role.Id)
                {
                    await Context.Channel.SendErrorAsync("Role already set as a tiered role.");
                    return;
                }
                _creds.PremiumRoleId = role.Id;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Premium role set {role.Mention}");
                break;
        }
    }
    
    [Command("setemotes")]
    [Summary("Sets the emotes for the bot")]
    [Usage("setemotes success <emote>")]
    [IsOwner]
    public async Task SetEmotes(EmoteType type, [Remainder] string emote2)
    {
        switch (type)
        {
            case EmoteType.Success:
                if (emote2.ToIEmote() is null)
                {
                    await Context.Channel.SendErrorAsync("Emote not found.");
                    return;
                }
                _creds.SuccessEmote = emote2;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Success emote set to {emote2}");
                break;
            case EmoteType.Loading:
                if (emote2.ToIEmote() is null)
                {
                    await Context.Channel.SendErrorAsync("Emote not found.");
                    return;
                }
                _creds.LoadingEmote = emote2;
                SerializeYml.Serialize(_creds);
                await Context.Channel.SendConfirmAsync($"Loading emote set to {emote2}");
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