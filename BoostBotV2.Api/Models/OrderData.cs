namespace BoostBotV2.Api.Models;

public class OrderData
{
    public string Id { get; set; }
    public string CustomerEmail { get; set; }
    public int Quantity { get; set; }
    public string Gateway { get; set; }
    public string ProductTitle { get; set; }
}