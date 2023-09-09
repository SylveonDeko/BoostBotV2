using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Common.AutoCompleters;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.SlashModule;

[Group("blacklist", "Blacklists a user or guild from using the bot.")]
public class Blacklist : BoostInteractionModuleBase
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

    [SlashCommand("add-user", "Blacklists a user from using the bot.")]
    [IsOwner]
    public async Task BlacklistUserAsync(IUser user)
    {
        await DeferAsync();
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == user.Id);
        
        if (_creds.Owners.Contains(user.Id))
        {
            await Context.Interaction.SendErrorAsync("You can't blacklist another owner.");
            return;
        }
        
        if (blacklisted is not null)
        {
            await Context.Interaction.SendErrorAsync("That user is already blacklisted.");
            return;
        }

        uow.Blacklists.Add(new Blacklists
        {
            BlacklistId = user.Id,
            BlacklistType = BlacklistType.User
        });
        await uow.SaveChangesAsync();
        await Context.Interaction.SendConfirmAsync("User blacklisted.");
    }
    
    [SlashCommand("add-guild", "Blacklists a guild from using the bot.")]
    [IsOwner]
    public async Task BlacklistGuildAsync([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
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
        await Context.Interaction.SendConfirmAsync("Guild blacklisted.");
    }
    
    [SlashCommand("remove-user", "Unblacklists a user from using the bot.")]
    [IsOwner]
    public async Task UnBlacklistUserAsync(IUser user)
    {
        await DeferAsync();
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == user.Id);
        
        if (blacklisted is null)
        {
            await Context.Interaction.SendErrorAsync("That user isn't blacklisted.");
            return;
        }

        uow.Blacklists.Remove(blacklisted);
        await uow.SaveChangesAsync();
        await Context.Interaction.SendConfirmAsync("User unblacklisted.");
    }
    
    [SlashCommand("remove-guild", "Unblacklists a guild from using the bot.")]
    [IsOwner]
    public async Task UnBlacklistGuildAsync([Autocomplete(typeof(GuildAutoComplete))] string guildName)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
        await using var uow = _db.GetDbContext();
        var blacklisted = uow.Blacklists.FirstOrDefault(x => x.BlacklistId == guildId);
        
        if (blacklisted is null)
        {
            await Context.Interaction.SendErrorAsync("That guild isn't blacklisted.");
            return;
        }

        uow.Blacklists.Remove(blacklisted);
        await uow.SaveChangesAsync();
        await Context.Interaction.SendConfirmAsync("Guild unblacklisted.");
    }
    
    [SlashCommand("list", "Lists the specified blacklist.")]
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
        
        
        await _interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(60)).ConfigureAwait(false);

        async Task<PageBuilder> PageFactory(int page)
        {
            await Task.CompletedTask;
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
    
    [ComponentInteraction("bluser:*", true), IsOwner]
    public async Task BlacklistUser(ulong userId)
    {
        await DeferAsync(true);
        await using var uow = _db.GetDbContext();
        var user = await uow.Blacklists.FirstOrDefaultAsync(x => x.BlacklistId == userId && x.BlacklistType == BlacklistType.User);
        if (user is not null)
        {
            await Context.Interaction.SendErrorAsync("User is already blacklisted.");
            return;
        }
        await uow.Blacklists.AddAsync(new Blacklists
        {
            BlacklistId = userId,
            BlacklistType = BlacklistType.User
        });
        await uow.SaveChangesAsync();
        await Context.Interaction.RespondAsync("User blacklisted.", ephemeral: true);
    }
    
    [ComponentInteraction("blguild:*", true), IsOwner]
    public async Task BlacklistGuild(ulong guildId)
    {
        await DeferAsync(true);
        await using var uow = _db.GetDbContext();
        var guild = await uow.Blacklists.FirstOrDefaultAsync(x => x.BlacklistId == guildId && x.BlacklistType == BlacklistType.Guild);
        if (guild is not null)
        {
            await Context.Interaction.SendErrorAsync("Guild is already blacklisted.");
            return;
        }
        await uow.Blacklists.AddAsync(new Blacklists
        {
            BlacklistId = guildId,
            BlacklistType = BlacklistType.Guild
        });
        await uow.SaveChangesAsync();
        await Context.Interaction.SendConfirmAsync("Guild blacklisted.");
    }
}