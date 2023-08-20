using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Common.Modals;
using BoostBotV2.Common.Yml;
using Discord.Interactions;
using Serilog;

namespace BoostBotV2.Interactions;

public class Rebrand : InteractionModuleBase
{
    [ComponentInteraction("rebrand"), IsOwner]
    public async Task SendRebrandModel()
    {
        await Context.Interaction.RespondWithModalAsync<RebrandModal>("rebrandmodel");
    }
    
    [ModalInteraction("rebrandmodel")]
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
            
            SerializeYml.Serialize(creds, $"~/{clientId}/BoostBotV2/creds.yml");
            Extensions.ExecuteCommand($"cp tokens.txt ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp onlinetokens.txt ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db-shm ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cp BoostBot.db-wal ~/{clientId}/BoostBotV2/");
            Extensions.ExecuteCommand($"cd ~/{clientId}/BoostBotV2 && pm2 start --name {clientId} dotnet run");
            await Context.Interaction.RespondAsync("Rebrand complete.", ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error("Error rebranding: {e}", e);
        }
    }
}