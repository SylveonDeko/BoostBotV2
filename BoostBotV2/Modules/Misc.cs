using System.Diagnostics;
using System.Text;
using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Serilog;

#pragma warning disable CS8604

namespace BoostBotV2.Modules;

public class Misc : BoostModuleBase
{
    private readonly DiscordSocketClient _client;
    private readonly Credentials _creds;
    private readonly CommandService _commandService;
    private readonly DbService _db;
    private readonly InteractiveService _interactive;
    private readonly IServiceProvider _services;

    public Misc(DiscordSocketClient client, Credentials creds, CommandService commandService, DbService db, InteractiveService interactive, IServiceProvider services)
    {
        _client = client;
        _creds = creds;
        _commandService = commandService;
        _db = db;
        _interactive = interactive;
        _services = services;
    }

    [Command("help")]
    [Summary("Displays help.")]
    [Usage("help <command>\nhelp")]
    public async Task HelpAsync([Remainder] string command = null)
    {
        if (command is not null)
        {
            var cmd = _commandService.Commands.FirstOrDefault(x => string.Equals(x.Name, command, StringComparison.InvariantCultureIgnoreCase));
            if (cmd is null)
            {
                await Context.Channel.SendErrorAsync("Command not found.");
                return;
            }

            var attrib = (OwnerOnlyHelp)cmd.Attributes.FirstOrDefault(x => x is OwnerOnlyHelp);
            var usageAttrib = (Usage)cmd.Attributes.FirstOrDefault(x => x is Usage);
            var membersAttrib = (IsMembersChannel)cmd.Preconditions.FirstOrDefault(x => x is IsMembersChannel);
            var ownerAttrib = (IsOwner)cmd.Preconditions.FirstOrDefault(x => x is IsOwner);
            var premAttrib = (IsPremium)cmd.Preconditions.FirstOrDefault(x => x is IsPremium);

            if (attrib is not null && !_creds.Owners.Contains(Context.User.Id))
            {
                await Context.Channel.SendErrorAsync("Command not found.");
                return;
            }

            var eb = new EmbedBuilder()
                .WithTitle($"{_creds.Prefix}{command}")
                .WithColor(Color.Purple)
                .WithDescription($"{cmd.Summary}");

            var newString = new StringBuilder();

            if (membersAttrib is not null)
                newString.Append("Members Channel Only\n");

            if (ownerAttrib is not null)
                newString.Append("Owner Only\n");

            if (premAttrib is not null)
                newString.Append("Premium Only\n");

            if (newString.Length > 0)
                eb.AddField("Requirements", newString.ToString());

            if (usageAttrib is not null)
            {
                eb.AddField("Usage", $"{string.Join("\n", usageAttrib.UsageString.Select(x => $"`{_creds.Prefix}{x}`"))}");
            }

            await ReplyAsync(embed: eb.Build());
            return;

        }
        
        var modules = _commandService.Modules;
        var scond = new EmbedBuilder()
            .WithColor(Color.Purple)
            .WithTitle($"{_client.CurrentUser.Username} Help Menu");
        
        foreach (var module in modules.Where(x => x.Commands.Any()))
        {
            HashSet<CommandInfo> commandInfos = new();
            foreach (var commandInfo in module.Commands)
            {
                var isAllowed = await commandInfo.CheckPreconditionsAsync(Context, _services);
                if (isAllowed.IsSuccess)
                    commandInfos.Add(commandInfo);
            }
            

            if (commandInfos.Any())
                scond.AddField(module.Name, string.Join(", ", commandInfos.Select(x => $"`{x.Name}`")));
        }

        await ReplyAsync(embed: scond.Build());
    }

    [Command("leaveinactive")]
    [Usage("leaveinactive")]
    [Summary("Leaves all inactive servers that havent added members in the last 48h")]
    [IsOwner]
    public async Task LeaveInactive()
    {
        HashSet<SocketGuild> toLeave = new();
        await using var uow = _db.GetDbContext();
        var guilds = uow.GuildsAdded.Where(x => x.DateAdded > DateTime.UtcNow.AddDays(-2)).ToHashSet();
        var actualGuilds = _client.Guilds;
        foreach (var i in actualGuilds)
        {
            if (!guilds.Select(x => x.GuildId).Contains(i.Id))
                toLeave.Add(i);
        }

        foreach (var toAdd in guilds.Select(i => _client.GetGuild(i.GuildId)).Where(toAdd => toAdd is not null))
        {
            toLeave.Add(toAdd);
        }

        var isNull = toLeave.FirstOrDefault(x => x.Id == 1123845653299216414);
        if (isNull is not null)
        {
            Log.Information("Got here");
            toLeave.Remove(isNull);
        }

        if (await PromptUserConfirmAsync($"Are you sure you want to leave {toLeave.Count} guilds?", Context.User.Id))
        {
            await Context.Channel.SendErrorAsync("Leaving guilds...");
            foreach (var guild in toLeave)
            {
                await guild.LeaveAsync();
            }

            await Context.Channel.SendErrorAsync("Done.");
        }

    }

    [Command("leave")]
    [Usage("leave serverid")]
    [Summary("Leaves the server you mentioned")]
    [IsOwner]
    public async Task Leave(ulong id)
    {
        var guild = _client.GetGuild(id);
        if (guild is null)
        {
            await Context.Channel.SendErrorAsync("Guild not found.");
            return;
        }

        if (await PromptUserConfirmAsync($"Are you sure you want to leave {guild.Name}?", Context.User.Id))
        {
            await guild.LeaveAsync();
            await Context.Channel.SendErrorAsync("Done.");
        }
    }
    
    [Command("listservers")]
    [Usage("listservers")]
    [Summary("Lists all servers the bot is in.")]
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

        await _interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(60)).ConfigureAwait(false);

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
    
    [Command("addbot")]
    [Usage("addbot")]
    [Summary("Provides a link to add this bot to your server.")]
    public async Task AddBotAsync()
    {
        var components = new ComponentBuilder()
            .WithButton("Add To Server", style: ButtonStyle.Link, url: $"https://discord.com/oauth2/authorize?client_id={_client.CurrentUser.Id}&scope=bot&permissions=1")
            .Build();
        var eb = new EmbedBuilder()
            .WithColor(Color.Purple)
            .WithTitle(Context.Client.CurrentUser.ToString())
            .WithDescription($"Add {Context.Client.CurrentUser} to your server using the button below:");
        await ReplyAsync(embed: eb.Build(), components: components);
    }

    [Command("stats")]
    [Usage("stats")]
    [Summary("Displays bot stats.")]
    public async Task Stats()
    {
        await using var uow = _db.GetDbContext();
        var eb = new EmbedBuilder()
            .WithAuthor("BoostBot v3", _client.CurrentUser.GetAvatarUrl(), "https://discord.gg/edotbaby")
            .AddField("Author", "<@967038397715709962>")
            .AddField("Owners", string.Join("\n", _creds.Owners.Select(x => $"<@{x}>")))
            .AddField("Guilds", _client.Guilds.Count)
            .AddField("Users", _client.Guilds.Sum(x => x.MemberCount))
            .AddField("Channels", _client.Guilds.Sum(x => x.Channels.Count))
            .AddField("Commands", _commandService.Commands.Count())
            .AddField("Total Added Members", uow.GuildsAdded.Count())
            .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).Days} days, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Hours} hours, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Minutes} minutes, {(DateTime.Now - Process.GetCurrentProcess().StartTime).Seconds} seconds")
            .WithColor(Color.Purple);
        
        await ReplyAsync(embed: eb.Build());
    }
}