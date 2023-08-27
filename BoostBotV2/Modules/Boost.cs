using System.Text.RegularExpressions;
using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using BoostBotV2.Services;
using Discord;
using Discord.Commands;

namespace BoostBotV2.Modules;

public partial class Boost : BoostModuleBase
{
    private readonly DiscordAuthService _discordAuthService;
    private readonly DbService _db;
    private readonly HttpClient _httpClient;

    public Boost(DiscordAuthService discordAuthService, DbService db, HttpClient httpClient)
    {
        _discordAuthService = discordAuthService;
        _db = db;
        _httpClient = httpClient;
    }

    [IsOwner]
    [Summary("Boosts a server with your selected member count")]
    [Usage("boost <guild id> <member count>")]
    [Command("boost")]
    public async Task BoostAsync(ulong guildId, int tokenCount)
    {
        await using var uow = _db.GetDbContext();
        var guild = await Context.Client.GetGuildAsync(guildId);
        if (guild == null)
        {
            var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=1&scope=bot%20applications.commands";
            var button = new ComponentBuilder()
                .WithButton("Add To Server", style: ButtonStyle.Link, url: inviteUrl);
            var embed = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"You need to invite the bot to the server before using this command.")
                .WithColor(Color.Red)
                .Build();
            await Context.Message.ReplyAsync(embed: embed, components: button.Build());
            return;
        }
        
        
        var tokens = _discordAuthService.GetBoostTokens(Context.User.Id);
        
        if (!tokens.Any())
        {
            await Context.Channel.SendErrorAsync("No Stock.");
            return;
        }

        var successCount = 0;
        var tokenIndex = 0;
        var eb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription($"<a:loading:1136512164551741530> Boosting Server '{guild}' with {tokenCount} members....");
        var message = await Context.Message.ReplyAsync(embed: eb.Build());
        
        var usedTokens = new HashSet<string>();
        var guildUsedTokens = new HashSet<string>(uow.GuildsAdded.Where(x => x.GuildId == guild.Id).Select(x => x.Token));

        foreach (var token in tokens)
        {
            if (successCount >= tokenCount) 
                break;
            if (guildUsedTokens.Contains(token)) 
                continue;
            if (!await _discordAuthService.BoostAuthorizer(token, guild.Id.ToString())) continue;
            successCount += 1;
            usedTokens.Add(token);
        }

        if (successCount > 0)
        {
            var embed = new EmbedBuilder()
                .WithDescription($"✅ Succesfully boosted {guild} with {successCount} members.")
                .WithColor(Color.Green)
                .Build();
            await message.ModifyAsync(msg => msg.Embed = embed);
            await uow.GuildsAdded.AddRangeAsync(usedTokens.Select(x => new GuildsAdded { GuildId = guild.Id, Token = x }));
            await uow.SaveChangesAsync();
        }
        else
        {
            await Context.Channel.SendErrorAsync("Failed to boost. Most likely bad stock.");
        }
    }
    
    [Command("addnitrostock")]
    [Usage("addnitrostock <attachment>")]
    [Summary("Adds nitro stock to the bot. Stock is per user.")]
    public async Task AddNitroStockAsync()
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            await Context.Channel.SendErrorAsync("You need to attach a file.");
            return;
        }
        
        var contents = await _httpClient.GetStringAsync(attachment.Url);
        
        
        if (attachment == null)
        {
            await Context.Channel.SendErrorAsync("No attachment found.");
            return;
        }
        
        if (attachment.Filename.Split('.').LastOrDefault() != "txt")
        {
            await Context.Channel.SendErrorAsync("Invalid file type.");
            return;
        }
        
        var fileSplit = contents.Split("\n");

        if (fileSplit.Any(x => !MyRegex().IsMatch(x)))
        {
            await Context.Channel.SendErrorAsync("One or mode invalid members found.");
            return;
        }

        var tokens = contents.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x));
        
        if (!tokens.Any())
        {
            await Context.Channel.SendErrorAsync("No valid tokens found.");
            return;
        }
        
        foreach (var token in tokens)
        {
            await _discordAuthService.AddBoostToken(Context.User.Id, token);
        }

        await ReplyAsync($"Added {tokens.Count()} members to the bot.");
    }
    
    [GeneratedRegex(@"^[a-zA-Z0-9+/=]+\.[a-zA-Z0-9_-]{2,7}\.[a-zA-Z0-9_-]{5,40}$")]
    private static partial Regex MyRegex();
    
}