using BoostBotV2.Api.Models;

namespace BoostBotV2.Api.Services
{
    public interface IWebhookService
    {
        string ProcessWebhookData(SellixWebhookData data);
        void SendDiscordNotification(string orderId, string email, int amount, string gateway, int quantity, string key);
        string GenerateKey(int length = 16);
        void SaveStateToJson(string key, object stateData);
    }
}