using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using Discord;
using Discord.Commands;

namespace BoostBotV2.Modules;

public class OwnerOnly : BoostModuleBase
{
    private readonly Credentials _creds;
    private readonly DbService _db;

    public OwnerOnly(Credentials creds, DbService db)
    {
        _creds = creds;
        _db = db;
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
}