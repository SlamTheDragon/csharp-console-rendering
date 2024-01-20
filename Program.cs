using Utilities;
using RenderEngine;

namespace Program;

internal class Program : Configure
{
    private readonly Logger Log = new("Main");
    private readonly Renderer renderer = new();

    public static async Task Main(string[] args)
    {
        Program program = new();

        // Check if command-line arguments are provided
        if (args.Length > 0)
        {
            program.Welcome();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "help":
                        Console.WriteLine("Define args help here. Press enter to exit");
                        Console.ReadLine();
                        program.Cleanup(0);
                        return;
                    default:
                        break;
                }
            }
            await program.Start();
        }
        else
        {
            program.Welcome();
            await program.Start();
        }

        program.Cleanup(0);
    }

    private async Task Start()
    {
        await renderer.Initialize();
    }

    // move this somewhere else
    private void Welcome()
    {
        if (Properties == null) return;
        Log.Info("[==========================================]");
        Log.Info("  Welcome to Slam's Fun Console Rendering");
        Log.Info($"               Project {Properties.Release}");
        Log.Info("[==========================================]");
    }

    public void Cleanup(int exitCode)
    {
        Console.Clear();

        Log.Info("Exiting Application...", true);

        Logger.Cleanup();
        Save();

        Environment.Exit(exitCode);
    }
}