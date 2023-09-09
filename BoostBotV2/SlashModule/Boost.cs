using System.Text.RegularExpressions;
using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Common.AutoCompleters;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using BoostBotV2.Services;
using Discord;
using Discord.Interactions;

namespace BoostBotV2.SlashModule;

[Group("boost", "Boosts stuff!")]
public partial class Boost : BoostInteractionModuleBase
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
    [SlashCommand("boostserver", "Boosts a server.")]
    public async Task BoostAsync([Autocomplete(typeof(GuildAutoComplete))] string guildName, int tokenCount)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildName, out var guildId))
        {
            await Context.Interaction.SendErrorAsync("That is not a valid server ID.");
            return;
        }
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
            await Context.Interaction.FollowupAsync(embed: embed, components: button.Build());
            return;
        }
        
        
        var tokens = _discordAuthService.GetBoostTokens(Context.User.Id);
        
        if (!tokens.Any())
        {
            await Context.Interaction.SendErrorAsync("No Stock.");
            return;
        }

        var successCount = 0;
        var tokenIndex = 0;
        var eb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription($"<a:loading:1136512164551741530> Boosting Server '{guild}' with {tokenCount} members....");
        var message = await Context.Interaction.FollowupAsync(embed: eb.Build());
        
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
            await Context.Interaction.SendErrorAsync("Failed to boost. Most likely bad stock.");
        }
    }
    
    [SlashCommand("addstock", "Adds nitro stock to the bot. Stock is per user.")]
    public async Task AddNitroStockAsync(IAttachment attachment)
    {

        var contents = await _httpClient.GetStringAsync(attachment.Url);
        
        
        if (attachment == null)
        {
            await Context.Interaction.SendErrorAsync("No attachment found.");
            return;
        }
        
        if (attachment.Filename.Split('.').LastOrDefault() != "txt")
        {
            await Context.Interaction.SendErrorAsync("Invalid file type.");
            return;
        }
        
        var fileSplit = contents.Split("\n");

        if (fileSplit.Any(x => !MyRegex().IsMatch(x)))
        {
            await Context.Interaction.SendErrorAsync("One or mode invalid members found.");
            return;
        }

        var tokens = contents.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x));
        
        if (!tokens.Any())
        {
            await Context.Interaction.SendErrorAsync("No valid tokens found.");
            return;
        }
        
        foreach (var token in tokens)
        {
            await _discordAuthService.AddBoostToken(Context.User.Id, token);
        }

        await Context.Interaction.SendConfirmAsync($"Added {tokens.Count()} members to the bot.");
    }
    
    [GeneratedRegex(@"^[a-zA-Z0-9+/=]+\.[a-zA-Z0-9_-]{2,7}\.[a-zA-Z0-9_-]{5,40}$")]
    private static partial Regex MyRegex();
    
}