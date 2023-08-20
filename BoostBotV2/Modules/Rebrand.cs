using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using Discord;
using Discord.Commands;

namespace BoostBotV2.Modules;

public class Rebrand : BoostModuleBase
{
    [IsOwner]
    [Command("rebrand")]
    [Usage("rebrand")]
    [Summary("Rebrands the bot.")]
    public async Task RebrandAsync()
    {
        var nodeVersion = Extensions.ExecuteCommand("node -v");
        if (!string.IsNullOrWhiteSpace(nodeVersion))
        {
            await Context.Channel.SendErrorAsync("Node is not installed. Install it and try again.");
            return;
        }
        
        var pm2Details = Extensions.ExecuteCommand("npm list -g pm2");
        if (!pm2Details.Contains("pm2@"))
        {
            await Context.Channel.SendErrorAsync("PM2 is not installed. Install it using `npm install -g pm2@3.1.3` and try again.");
            return;
        }
        var components = new ComponentBuilder()
            .WithButton("Press here to get started", "rebrand", ButtonStyle.Success)
            .Build();
        
        await ReplyAsync("_ _", components: components);
    }
}