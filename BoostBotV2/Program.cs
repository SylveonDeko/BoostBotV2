using Serilog;
using BoostBotV2;
using Figgle;

var pid = Environment.ProcessId;

Console.WriteLine(
    FiggleFonts.Ogre.Render("BoostBot V3"));

LogSetup.SetupLogger("BoostBotV3");
Log.Information($"Pid: {pid}");

await new Bot().RunAndBlockAsync();