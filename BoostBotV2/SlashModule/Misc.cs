using System.Diagnostics;
using BoostBotV2.Common;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
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
    private readonly DbService _db;

    public Misc(DiscordSocketClient client, Credentials creds, CommandService commands, DbService db)
    {
        _client = client;
        _creds = creds;
        _commands = commands;
        _db = db;
    }

    [SlashCommand("addbot", "Add the bot to your server")]
    public async Task AddBotAsync()
    {
        await DeferAsync();
        var components = new ComponentBuilder()
            .WithButton("Add To Server", style: ButtonStyle.Link, url: $"https://discord.com/oauth2/authorize?client_id={_client.CurrentUser.Id}&scope=bot&permissions=1")
            .Build();
        var eb = new EmbedBuilder()
            .WithColor(Color.Purple)
            .WithTitle(Context.Client.CurrentUser.ToString())
            .WithDescription($"Add {Context.Client.CurrentUser} to your server using the button below:");
        await Context.Interaction.FollowupAsync(embed: eb.Build(), components: components);
    }

    [SlashCommand("stats", "Get bot stats")]
    public async Task Stats()
    {
        await DeferAsync();
        await using var uow = _db.GetDbContext();
        var eb = new EmbedBuilder()
            .WithAuthor("BoostBot v3", _client.CurrentUser.GetAvatarUrl(), "https://discord.gg/edotbaby")
            .AddField("Author", "<@967038397715709962>")
            .AddField("Owners", string.Join("\n", _creds.Owners.Select(x => $"<@{x}>")))
            .AddField("Guilds", _client.Guilds.Count)
            .AddField("Users", _client.Guilds.Sum(x => x.MemberCount))
            .AddField("Channels", _client.Guilds.Sum(x => x.Channels.Count))
            .AddField("Commands", _commands.Commands.Count())
            .AddField("Total Added Members", uow.GuildsAdded.Count())
            .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).Days} days, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Hours} hours, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Minutes} minutes, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Seconds} seconds")
            .WithColor(Color.Purple);
        
        await Context.Interaction.FollowupAsync(embed: eb.Build());
    }
}