#nullable disable
namespace BoostBotV2.Common.Yml;

public class Credentials
{
    public string BotToken { get; set; }
    public ulong CommandLogChannel { get; set; }
    public ulong CommandGuild { get; set; }
    public string ClientSecret { get; set; }
    public ulong ClientId { get; set; }
    public ulong FarmChannel { get; set; }
    public string Prefix { get; set; }
    
    public List<ulong> Owners { get; set; }
}