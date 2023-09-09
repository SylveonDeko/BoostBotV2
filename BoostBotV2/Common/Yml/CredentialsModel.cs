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
    public bool RequireAgreement { get; set; }
    public ulong FreeRoleId { get; set; } = 1133547572603146280;
    public ulong BronzeRoleId { get; set; } = 1133565792332554271;
    public ulong SilverRoleId { get; set; } = 1133605697909698650;
    public ulong GoldRoleId { get; set; } = 1133593021569585284;
    public ulong PlatinumRoleId { get; set; } = 1133960226534588446;
    public ulong PremiumRoleId { get; set; } = 1136525445693706370;
    public string LoadingEmote { get; set; } = "<a:loading:1136512164551741530>";
    public string SuccessEmote { get; set; } = "✅";
    public string StoreLink { get; set; }
    public int? CorrectRule { get; set; }
    public List<string>? Rules { get; set; }
    public List<ulong> Owners { get; set; }
}