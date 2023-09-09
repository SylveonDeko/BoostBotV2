using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Common.Modals;
using BoostBotV2.Common.Yml;
using Discord;
using Discord.Interactions;
using Serilog;

namespace BoostBotV2.SlashModule;

public class Rebrand : BoostInteractionModuleBase
{
    [IsOwner]
    [SlashCommand("rebrand", "Makes a copy of the bot and rebrands it")]
    public async Task RebrandAsync()
    {
        await DeferAsync();
        var nodeVersion = Extensions.ExecuteCommand("node -v");
        if (string.IsNullOrWhiteSpace(nodeVersion))
        {
            await Context.Interaction.SendErrorAsync("Node is not installed. Install it and try again.");
            return;
        }
        
        var pm2Details = Extensions.ExecuteCommand("npm list -g pm2");
        if (!pm2Details.Contains("pm2@"))
        {
            await Context.Interaction.SendErrorAsync("PM2 is not installed. Install it using `npm install -g pm2@3.1.3` and try again.");
            return;
        }
        var components = new ComponentBuilder()
            .WithButton("Press here to get started", "rebrandbutton", ButtonStyle.Success)
            .Build();
        
        await Context.Interaction.FollowupAsync("_ _", components: components);
    }
    
    [ComponentInteraction("rebrandbutton", true), IsOwner]
    public async Task SendRebrandModel()
    {
        await Context.Interaction.RespondWithModalAsync<RebrandModal>("rebrandmodel");
    }
    
    [ModalInteraction("rebrandmodel", true)]
    public async Task RebrandModal(RebrandModal modal)
    {
        try
        {
            if (modal.Token is null || modal.ClientId is null || modal.ClientSecret is null || modal.Owners is null)
            {
                await Context.Interaction.RespondAsync("You must fill out all fields.");
                return;
            }

            if (!ulong.TryParse(modal.ClientId, out var clientId))
            {
                await Context.Interaction.RespondAsync("Client ID must be a number.", ephemeral: true);
            }
        
            if (!ulong.TryParse(modal.CommandGuild, out var commandGuild))
            {
                await Context.Interaction.RespondAsync("Command Guild must be a number.", ephemeral: true);
            }
        
            if (modal.Owners.Split(",").Any(x => !ulong.TryParse(x, out _)))
            {
                await Context.Interaction.RespondAsync("Owners must be a comma separated list of numbers.", ephemeral: true);
            }
        
            var creds = new Credentials
            {
                BotToken = modal.Token,
                ClientId = clientId,
                ClientSecret = modal.ClientSecret,
                CommandGuild = commandGuild,
                Prefix = "$",
                CommandLogChannel = 0,
                Owners = modal.Owners.Split(",").Select(x => ulong.Parse(x)).ToList()
            };
            
            Extensions.ExecuteCommand($"git clone git@github.com:SylveonDeko/BoostBotV2 ~/{clientId}");
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            SerializeYml.Serialize(creds, $"{homePath}/{clientId}/BoostBotV2/creds.yml");
            Extensions.ExecuteCommand($"cp tokens.txt ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp onlinetokens.txt ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db-shm ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db-wal ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cd ~/{clientId}/BoostBotV2 && pm2 start --name {clientId} \"dotnet run\"");
            await Context.Interaction.RespondAsync("Rebrand complete.", ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error("Error rebranding: {e}", e);
        }
    }
}