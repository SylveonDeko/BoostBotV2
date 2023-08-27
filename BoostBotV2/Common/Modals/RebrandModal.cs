using Discord.Interactions;

namespace BoostBotV2.Common.Modals;

public class RebrandModal : IModal
{
    public string Title { get; } = "Rebrand Info";
    
    [ModalTextInput("token")]
    [InputLabel("Bot Token")]
    public string Token { get; set; }
    
    [ModalTextInput("clientid")]
    [InputLabel("Client ID")]
    public string ClientId { get; set; }
    
    [ModalTextInput("clientsecret")]
    [InputLabel("Client Secret")]
    public string ClientSecret { get; set; }
    
    [ModalTextInput("commandguild")]
    [InputLabel("Command Guild (It's Id)")]
    public string CommandGuild { get; set; }
    
    [ModalTextInput("owners")]
    [InputLabel("Owners (Comma separated list of Ids)")]
    public string Owners { get; set; }
}