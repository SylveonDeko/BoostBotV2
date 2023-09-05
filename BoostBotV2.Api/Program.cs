using Figgle;

namespace BoostBotV2.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(
                FiggleFonts.DancingFont.Render("BoostBot.Api V1"));
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}