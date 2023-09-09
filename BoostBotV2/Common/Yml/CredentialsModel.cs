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
    public ulong FreeRoleId { get; set; }
    public ulong BronzeRoleId { get; set; }
    public ulong SilverRoleId { get; set; }
    public ulong GoldRoleId { get; set; }
    public ulong PlatinumRoleId { get; set; }
    public ulong PremiumRoleId { get; set; }
    public string StoreLink { get; set; }
    public int? CorrectRule { get; set; }
    public List<string>? Rules { get; set; }
    public List<ulong> Owners { get; set; }
}