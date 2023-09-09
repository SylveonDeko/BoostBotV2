using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace BoostBotV2.Common.AutoCompleters;

public class GuildAutoComplete : AutocompleteHandler
{
    private readonly DiscordSocketClient _client;

    public GuildAutoComplete(DiscordSocketClient cache)
    {
        _client = cache;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        try
        {
            var inpt = autocompleteInteraction.Data.Current.Value as string;
            var guilds = _client.Guilds;
            return AutocompletionResult.FromSuccess(guilds.Where(x => x.Name.ToLower().Contains(inpt?.ToLower()!)).Take(25).Select(x => new AutocompleteResult(x.Name.TrimTo(100, true), x.Id.ToString())));

        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error in GuildAutoComplete");
        }
        return AutocompletionResult.FromSuccess(new List<AutocompleteResult> { });
    }
}