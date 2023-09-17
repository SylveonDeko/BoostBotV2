using Serilog;
using BoostBotV2;
using Figgle;

var pid = Environment.ProcessId;

Console.WriteLine(
    FiggleFonts.Ogre.Render("DekAIO V3"));

LogSetup.SetupLogger("DekAIO V3");
Log.Information($"Pid: {pid}");

await new Bot().RunAndBlockAsync();