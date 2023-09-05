using BoostBotV2.Api.Models;
using BoostBotV2.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoostBotV2.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SellixController : ControllerBase
    {
        private readonly IWebhookService _webhookService;

        public SellixController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost]
        public IActionResult HandleWebhook([FromBody] SellixWebhookData data)
        {
            var response = _webhookService.ProcessWebhookData(data);
            return Ok(response);
        }
    }
}