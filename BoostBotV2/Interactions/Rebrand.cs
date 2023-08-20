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

            var currentDirectory = Directory.GetCurrentDirectory();
            string oldPath;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                oldPath = Path.GetFullPath("../../../../../BoostBotV2", currentDirectory);
            else
                oldPath = Path.GetFullPath("../../BoostBotV2", currentDirectory);
            var newPath = oldPath.Replace("BoostBotV2", clientId.ToString());
            CopyDirectory(oldPath, newPath);
            SerializeYml.Serialize(creds, newPath + "/BoostBotV2/creds.yml");
            await Context.Interaction.RespondAsync("Rebrand complete.", ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error("Error rebranding: {e}", e);
        }
    }

    private static void CopyDirectory(string sourceDirName, string destDirName)
    {
        var dir = new DirectoryInfo(sourceDirName);
        var dirs = dir.GetDirectories();

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        var files = dir.GetFiles();
        foreach (var file in files)
        {
            var tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, false);
        }

        foreach (var subdir in dirs)
        {
            var tempPath = Path.Combine(destDirName, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}