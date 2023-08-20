using Serilog;
using BoostBotV2;
using Figgle;

var pid = Environment.ProcessId;

Console.WriteLine(
    FiggleFonts.Ogre.Render("BoostBot V2.1"));

LogSetup.SetupLogger("BoostBotV2.1");
Log.Information($"Pid: {pid}");

await new Bot().RunAndBlockAsync();