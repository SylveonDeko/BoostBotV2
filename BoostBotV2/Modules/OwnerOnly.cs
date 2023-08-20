using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using Discord;
using Discord.Commands;

namespace BoostBotV2.Modules;

public class OwnerOnly : BoostModuleBase
{
    private readonly Credentials _creds;

    public OwnerOnly(Credentials creds)
    {
        _creds = creds;
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
}