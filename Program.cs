using Utilities;
using Utilities.Data;
using RenderEngine;

namespace Program;
internal static class Program
{
    private static readonly LogStreamer LogStreamer = new();
    private static Logger? Log;
    private static SharedData? Data;

    public static async Task Main(string[] args)
    {
        Data = new();
        Log = new("main", LogStreamer, Data);
        Logger LogRenderer = new("renderer", LogStreamer, Data);

        RendererTimings Timings = new(Data);
        Renderer Renderer = new Renderer()
            .AddLogger(LogRenderer)
            .AddData(Data)
            .AddTimingsManager(Timings);

        Log.Info("[==========================================]");
        Log.Info("  Welcome to Slam's Fun Console Rendering");
        Log.Info($"               Project {Data.Properties.Release}");
        Log.Info("[==========================================]");

        await Renderer.Initialize();

        Cleanup(0);
    }

    public static void Cleanup(int exitCode)
    {
        Console.Clear();
        Log.Info($"Exiting Application with exit code: {exitCode}", true);

        LogStreamer.Cleanup();
        Data.Save();

        Environment.Exit(exitCode);
    }
}