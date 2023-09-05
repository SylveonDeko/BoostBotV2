using System.Text;
using BoostBotV2.Api.Common.PubSub;
using BoostBotV2.Api.Extensions;
using BoostBotV2.Api.Models;
using BoostBotV2.Common.Yml;
using Discord;
using Discord.Webhook;
using Newtonsoft.Json;

namespace BoostBotV2.Api.Services.Impl
{
    public class WebhookService : IWebhookService
    {
        
        private readonly IPubSub _pubSub;
        private readonly TypedKey<bool> blPrivKey = new("blacklist.reload.priv");

        public WebhookService(IPubSub pubSub, Credentials credentials)
        {
            _pubSub = pubSub;
        }

        public string ProcessWebhookData(SellixWebhookData data)
        {
            // Extract data and generate the key and Discord link
            var key = GenerateKey(4);
            var clientId = "1145508050694848512"; // This seems to be constant in your code
            var scope = "identify bot";

            var botLink = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&response_type=code&scope={scope}&state={key}";

            // Sending the Discord notification
            SendDiscordNotification(data.Data.Id, data.Data.CustomerEmail, data.Data.Quantity, data.Data.Gateway, data.Data.Quantity, key);

            // Construct and return the response message
            var responseMessage = $"Key: {key}\nBot Link: {botLink}\n\n..."; // Continue the message based on your Python code
            return responseMessage;
        }

        public async void SendDiscordNotification(string orderId, string email, int amount, string gateway, int quantity, string key)
        {
            var webhookClient = new DiscordWebhookClient("https://discordapp.com/api/webhooks/1146276308322619393/SoDrh7ivHdJ9uWs7pXm3HPcsHBp5Co_ldci0UytQLJSHvYmFLLxt0hqYD0U18NxLCDY0");
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("New Order")
                .WithDescription($"Order ID: {orderId}\nEmail: {email}\nAmount: {amount}\nGateway: {gateway}\nQuantity: {quantity}\nKey: {key}")
                .Build();
            await webhookClient.SendMessageAsync(embeds: new[] { embed });
        }

        public string GenerateKey(int length = 16)
        {
            return KeyGenerator.GenerateKey(length);
        }

        public void SaveStateToJson(string key, object stateData)
        {
            var json = JsonConvert.SerializeObject(stateData, Formatting.Indented);
            File.WriteAllText($"path_to_keys_directory/keys.json", json);
        }
    }
}
