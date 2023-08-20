using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Microsoft.VisualBasic;

namespace BoostBotV2.Modules;

public class Blacklist : BoostModuleBase
{
    private readonly DbService _db;
    private readonly Credentials _creds;
    private readonly InteractiveService _interactive;
    private readonly DiscordSocketClient _client;

    public Blacklist(DbService db, Credentials creds, InteractiveService interactive, DiscordSocketClient client)
    {
        _db = db;
        _creds = creds;
        _interactive = interactive;
        _client = client;
    }

    [Command("blacklistuser")]
    [Summary("Blacklists a user from using the bot.")]
    [Usage("blacklistuser <user id>")]
    [IsOwner]
    public async Task BlacklistUserAsync(ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == userId);
        
        if (_creds.Owners.Contains(userId))
        {
            await ReplyAsync("You can't blacklist another owner.");
            return;
        }
        
        if (blacklisted is not null)
        {
            await ReplyAsync("That user is already blacklisted.");
            return;
        }

        uow.Blacklists.Add(new Blacklists
        {
            BlacklistId = userId,
            BlacklistType = BlacklistType.User
        });
        await uow.SaveChangesAsync();
        await ReplyAsync("User blacklisted.");
    }
    
    [Command("blacklistguild")]
    [Summary("Blacklists a guild from using the bot.")]
    [Usage("blacklistguild <guild id>")]
    [IsOwner]
    public async Task BlacklistGuildAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == guildId);
        
        if (blacklisted is not null)
        {
            await ReplyAsync("That guild is already blacklisted.");
            return;
        }

        uow.Blacklists.Add(new Blacklists
        {
            BlacklistId = guildId,
            BlacklistType = BlacklistType.Guild
        });
        await uow.SaveChangesAsync();
        await ReplyAsync("Guild blacklisted.");
    }
    
    [Command("unblacklistuser")]
    [Summary("Unblacklists a user from using the bot.")]
    [Usage("unblacklistuser <user id>")]
    [IsOwner]
    public async Task UnBlacklistUserAsync(ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == userId);
        
        if (blacklisted is null)
        {
            await ReplyAsync("That user isn't blacklisted.");
            return;
        }

        uow.Blacklists.Remove(blacklisted);
        await uow.SaveChangesAsync();
        await ReplyAsync("User unblacklisted.");
    }
    
    [Command("unblacklistguild")]
    [Summary("Unblacklists a guild from using the bot.")]
    [Usage("unblacklistguild <guild id>")]
    [IsOwner]
    public async Task UnBlacklistGuildAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == guildId);
        
        if (blacklisted is null)
        {
            await ReplyAsync("That guild isn't blacklisted.");
            return;
        }

        uow.Blacklists.Remove(blacklisted);
        await uow.SaveChangesAsync();
        await ReplyAsync("Guild unblacklisted.");
    }
    
    [Command("blacklistlist")]
    [Summary("Lists the specified blacklist.")]
    [Usage("blacklistlist type")]
    [IsOwner]
    public async Task BlacklistListAsync(BlacklistType type)
    {
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.Where(x => x.BlacklistType == type).ToHashSet();
        var paginator = new LazyPaginatorBuilder()
            .AddUser(Context.User)
            .WithPageFactory(PageFactory)
            .WithFooter(PaginatorFooter.PageNumber | PaginatorFooter.Users)
            .WithMaxPageIndex(blacklisted.Count / 10 == 0 ? 1 : blacklisted.Count / 10)
            .WithDefaultEmotes()
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .Build();
        
        
        await _interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(60)).ConfigureAwait(false);

        async Task<PageBuilder> PageFactory(int page)
        {
            if (type == BlacklistType.Guild)
            {
                var blacklists = blacklisted.Skip(page * 10).Take(10);
                var pageBuilder = new PageBuilder()
                    .WithTitle("Blacklisted Guilds")
                    .WithDescription($"{string.Join("\n", blacklists.Select(x => $"{x.BlacklistId}"))}")
                    .WithColor(Color.Red);

                return pageBuilder;
            }
            else
            {
                var blacklists = blacklisted.Skip(page * 10).Take(10).Select(x => _client.Rest.GetUserAsync(x.BlacklistId).GetAwaiter().GetResult());
                var pageBuilder = new PageBuilder()
                    .WithTitle("Blacklisted Users")
                    .WithDescription($"{string.Join("\n", blacklists.Select(x => $"{x.Username}: {x.Id}"))}")
                    .WithColor(Color.Red);

                return pageBuilder;
            }
        }
    }
}