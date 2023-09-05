using BoostBotV2.Api.Common.PubSub;
using BoostBotV2.Api.Services;
using BoostBotV2.Api.Services.Impl;
using BoostBotV2.Common.Yml;
using Microsoft.OpenApi.Models;

namespace BoostBotV2.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Credentials = DeserializeYaml.CredsDeserialize(true);
            _cache = new ApiDataCache(Credentials);
        }

        public IConfiguration Configuration { get; }
        private Credentials Credentials { get; set; }
        private readonly ApiDataCache _cache;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTransient<IWebhookService, WebhookService>();
            services.AddSingleton(Credentials);
            services.AddSingleton(_cache);
            services.AddSingleton(_cache.Redis);
            services.AddTransient<ISeria, JsonSeria>();
            services.AddTransient<IPubSub, RedisPubSub>();

            // Register the Swagger generator
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Sellix Api", 
                    Version = "v1" 
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}