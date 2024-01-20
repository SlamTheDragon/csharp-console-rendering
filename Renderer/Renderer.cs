using Utilities;

namespace RenderEngine;

public class Renderer : Configure
{
    public enum RendererType
    {
        SETTINGS,
        POINTER,
        DVD,
        LINES1,
        LINES2,
        EXPLOSION,
        CUBE,
    }
    private enum RendererOverlay
    {
        NORMAL,
        DEBUG,
        DETAILED_DEBUG,
        HELP,
    }
    private ConsoleKeyInfo _keyInfo;
    private volatile RendererType renderSelection = RendererType.POINTER;
    private volatile RendererOverlay _overlay;
    private volatile bool _isExit = false;
    private volatile bool _isRefreshing = false;
    private volatile bool _isRefreshingPaused = true;
    private volatile int _width;
    private volatile int _height;
    public readonly Logger Log = new("renderer");


    // Initialize Renderer
    public async Task Initialize()
    {
        Log.Info("Starting Console Game Engine...", true);
        await BuildScreen();

        Task screen = Task.Run(() => Refresh());
        Task keyboard = Task.Run(() => KeyListener());

        await Task.WhenAll(screen, keyboard);
    }

    private static void WritePixel(string input)
    {
        Console.WriteLine(input);
    }

    private async Task BuildScreen()
    {
        Log.Info("> Building Screen", true);

        Log.Info("    > Obtaining Initial Console Size", true);
        _width = Console.WindowWidth;
        _height = Console.WindowHeight;
        Log.Verbose($"Initial Window Size: W:{_width} H:{_height}");

        Log.Info("    > Initializing Renderer Methods", true);
        // Renderer Methods Here

        Log.Info("> Starting After 1 Second...", true);
        await Task.Run(() => Thread.Sleep(1000));
    }

    private void RenderMethod(RendererType type)
    {
        switch (type)
        {
            case RendererType.SETTINGS:
                _isRefreshing = false;

                Log.Debug("SETTINGS");
                Settings();
                break;

            case RendererType.POINTER:
                _isRefreshing = false;
                Log.Debug("POINTER");
                Console.WriteLine(RendererType.POINTER);
                break;

            case RendererType.DVD:
                _isRefreshing = true;
                Log.Debug("DVD");
                Console.WriteLine(RendererType.DVD);
                break;

            case RendererType.LINES1:
                _isRefreshing = false;
                Log.Debug("LINES1");
                Console.WriteLine(RendererType.LINES1);
                break;

            case RendererType.LINES2:
                _isRefreshing = false;
                Log.Debug("LINES2");
                Console.WriteLine(RendererType.LINES2);
                break;

            case RendererType.EXPLOSION:
                _isRefreshing = true;
                Log.Debug("EXPLOSION");
                Console.WriteLine(RendererType.EXPLOSION);
                break;

            case RendererType.CUBE:
                _isRefreshing = true;
                Log.Debug("CUBE");
                Console.WriteLine(RendererType.CUBE);
                break;

            default:
                _isRefreshing = false;
                Log.Warn("Ending Process");

                _isExit = true;
                return;
        }
    }

    private async Task Refresh()
    {
        if (Properties == null) return;

        await Task.Run(async () =>
        {
            _isRefreshingPaused = true;
            _width = Console.WindowWidth;
            _height = Console.WindowHeight;
            Log.Verbose($"Window Refreshed: W:{_width} H:{_height}");

            Console.Clear();
            RenderMethod(renderSelection);

            if (_isRefreshing)
            {
                Thread.Sleep(1000 / Properties.Refresh);
            }
            else
            {
                // Pause block: Pauses until _isRefreshingPaused is unlatched)
                await Task.Run(() =>
                {
                    while (_isRefreshingPaused)
                    {
                        Thread.Sleep(Properties.Tick * 50);
                    }
                });
            }
        });

        if (_isExit) return;
        await Refresh();
    }

    private async Task KeyListener()
    {
        if (_isExit) return;
        if (Properties == null) return;

        await Task.Run(async () =>
        {

            if (renderSelection == RendererType.SETTINGS) { Thread.Sleep(Properties.Tick * 50); await KeyListener(); if (_isExit) return; }

            _keyInfo = Console.ReadKey(true);
            _isRefreshingPaused = false;
            CheckKeys();

            Log.Verbose($"Key: {_keyInfo.Key}");
            Thread.Sleep(Properties.Tick * 50);
        });

        await KeyListener();
    }

    private void CheckKeys()
    {
        switch (_keyInfo.Key)
        {
            case ConsoleKey.Escape:
                if (renderSelection == RendererType.POINTER) { _isExit = true; return; }
                renderSelection = RendererType.POINTER;
                break;

            case ConsoleKey.F1:
                _overlay = (_overlay == RendererOverlay.DEBUG)
                ? RendererOverlay.NORMAL
                : RendererOverlay.DEBUG;
                break;

            case ConsoleKey.F2:
                renderSelection = RendererType.SETTINGS;
                break;

            case ConsoleKey.F3:
                _overlay = (_overlay == RendererOverlay.DETAILED_DEBUG)
                ? RendererOverlay.NORMAL
                : RendererOverlay.DETAILED_DEBUG;
                break;

            case ConsoleKey.Q:
                _overlay = (_overlay == RendererOverlay.HELP)
                ? RendererOverlay.NORMAL
                : RendererOverlay.HELP;
                break;

            case ConsoleKey.D1:
                renderSelection = RendererType.POINTER;
                break;

            case ConsoleKey.D2:
                renderSelection = RendererType.DVD;
                break;

            case ConsoleKey.D3:
                renderSelection = RendererType.LINES1;
                break;

            case ConsoleKey.D4:
                renderSelection = RendererType.LINES2;
                break;

            case ConsoleKey.D5:
                renderSelection = RendererType.EXPLOSION;
                break;

            case ConsoleKey.D6:
                renderSelection = RendererType.CUBE;
                break;

            default:
                break;
        }
    }

    public void Settings()
    {
        string err = "";
        if (Properties == null) return;

        while (renderSelection == RendererType.SETTINGS)
        {
            Console.Clear();
            Console.WriteLine(err);
            Console.WriteLine("[ Settings ]\n");
            Console.WriteLine("1  -  Exit Settings");
            Console.WriteLine($"2  -  Change Target FPS ({Properties.Refresh})");
            Console.WriteLine($"3  -  Change Background Tick Rate ({Properties.Tick})\n");
            Console.WriteLine($"4  -  Toggle Debug Logging ({Properties.IsDebug})");
            Console.WriteLine($"5  -  Toggle Verbose Logging ({Properties.IsVerbose})\n");
            // reset var
            err = "";

            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    renderSelection = RendererType.POINTER;
                    _isRefreshingPaused = false;
                    break;

                case "2":
                    Properties.Refresh = Int32.Parse(SetProperty("Enter New Target FPS:"));
                    break;

                case "3":
                    
                    int test = Int32.Parse(SetProperty("Enter New Tick Rate (Maximum 20):"));
                    if (test > 20)
                    {
                        err = "Value is too high.";
                    } else
                    {
                        Properties.Tick = test;
                    }
                    break;

                case "4":
                    Properties.IsDebug = !Properties.IsDebug;
                    err = "Info: Restart Required";
                    break;

                case "5":
                    Properties.IsVerbose = !Properties.IsVerbose;
                    err = "Info: Restart Required";
                    break;

                default:
                    err = "Error: No cases matched.";
                    break;
            }
        }
    }

    public string SetProperty(string description)
    {
        bool isNull = false;

        while (renderSelection == RendererType.SETTINGS)
        {
            Console.Clear();
            if (isNull) { Console.WriteLine("Warning: Please enter something."); }
            else { Console.WriteLine(""); }

            Console.WriteLine($"{description} \n");

            var _ = Console.ReadLine();

            if (_ == null || _ == "")
            {
                isNull = true;
            }
            else
            {
                return _;
            }
        }

        return "";
    }

    private async Task HandleError(Exception e)
    {
        await Task.Run(() =>
        {
            Log.Warn(e);
        });
    }
}