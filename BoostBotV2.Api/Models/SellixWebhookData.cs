namespace BoostBotV2.Api.Models;

public class SellixWebhookData
{
    public string Event { get; set; }
    public OrderData Data { get; set; }
}