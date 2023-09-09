using System.Diagnostics;
using BoostBotV2.Common;
using BoostBotV2.Common.Yml;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

#pragma warning disable CS8604

namespace BoostBotV2.SlashModule;

[Discord.Interactions.Group("misc", "Misc Commands.")]
public class Misc : BoostInteractionModuleBase
{
    private readonly DiscordSocketClient _client;
    private readonly Credentials _creds;
    private readonly CommandService _commands;

    public Misc(DiscordSocketClient client, Credentials creds, CommandService commands)
    {
        _client = client;
        _creds = creds;
        _commands = commands;
    }

    [SlashCommand("addbot", "Add the bot to your server")]
    public async Task AddBotAsync()
    {
        var components = new ComponentBuilder()
            .WithButton("Add To Server", style: ButtonStyle.Link, url: $"https://discord.com/oauth2/authorize?client_id={_client.CurrentUser.Id}&scope=bot&permissions=1")
            .Build();
        var eb = new EmbedBuilder()
            .WithColor(Color.Purple)
            .WithTitle("Qsxt")
            .WithDescription("Add Qsxt to your server using the button below:");
        await ReplyAsync(embed: eb.Build(), components: components);
    }

    [SlashCommand("stats", "Get bot stats")]
    public async Task Stats()
    {
        var eb = new EmbedBuilder()
            .WithAuthor("BoostBot v3", _client.CurrentUser.GetAvatarUrl(), "https://discord.gg/edotbaby")
            .AddField("Author", "<@967038397715709962>")
            .AddField("Owners", string.Join("\n", _creds.Owners.Select(x => $"<@{x}>")))
            .AddField("Guilds", _client.Guilds.Count)
            .AddField("Users", _client.Guilds.Sum(x => x.MemberCount))
            .AddField("Channels", _client.Guilds.Sum(x => x.Channels.Count))
            .AddField("Commands", _commands.Commands.Count())
            .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).Days} days, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Hours} hours, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Minutes} minutes, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Seconds} seconds")
            .WithColor(Color.Purple);
        
        await ReplyAsync(embed: eb.Build());
    }
}