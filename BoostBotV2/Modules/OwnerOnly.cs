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
}