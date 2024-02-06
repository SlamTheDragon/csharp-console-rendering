using System.Text.RegularExpressions;
using Utilities;

namespace RenderEngine;

public class Renderer : Configure
{
    #region Renderer Properties
    /*****************************************************
        MAIN PROPERTIES
    *****************************************************/
    private enum RendererType
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
    private readonly Logger Log = new("renderer");
    private ConsoleKeyInfo _keyInfo;
    private volatile RendererType renderSelection = RendererType.POINTER;
    private volatile RendererOverlay _overlay;
    private volatile bool _isExit = false;
    private volatile bool _isRefreshing = false;
    private volatile bool _isRefreshingPaused = true;

    private Screen Screen;
    private ScreenInterfaceBuilder ScreenSettings;
    private ScreenInterfaceBuilder Menu;
    private readonly RendererTimings rendererTimings = new();
    #endregion

    #region Screen Builder
    public async Task Initialize()
    {
        Log.Info("Starting Console Game Engine...", true);
        Log.Info("> Building Screen", true);

        // Build GUI
        ScreenSettings = new ScreenInterfaceBuilder()
                    .AddText("Sample Text", 0, 0);

        Menu = new ScreenInterfaceBuilder()
                    .AddText("Sample Text 1", 0, 0)
                    .AddButton("Sample Button", 0, 1, 1)
                    .AddButton("Sample Button", 5, 2, 1)
                    .AddButton("Sample Button", 8, 3, 1)
                    .AddButton("Sample Button", 2, 4, 1);

        // Build Screen
        Screen = new Screen(Console.WindowWidth, Console.WindowHeight)
                    .AddGUI("Settings", ScreenSettings.GetData())
                    .AddGUI("MainMenu", Menu.GetData());

        Log.Verbose($"Initial Window Size: W:{Screen.width} H:{Screen.height}");
        Log.Info("    > Initializing Renderer Methods", true);
        Screen.BuildFrame();

        // Wait for 1 second to let the person read (possibly be removed)
        Log.Info("> Starting...", true);
        await Task.Run(() => Thread.Sleep(1000));


        Screen.ClearFrame();
        Task window = Task.Run(() => Refresh());
        Task keyboard = Task.Run(() => KeyListener());

        await Task.WhenAll(window, keyboard);
    }

    private void RebuildScreen()
    {
        _isRefreshingPaused = true;
        decimal baseWidth = (decimal)Console.WindowWidth / 2;
        int baseHeight = Console.WindowHeight - 1;

        if ((Screen.width != (int)Math.Floor(baseWidth)) || (Screen.height != baseHeight))
        {
            Screen = new Screen(Console.WindowWidth, Console.WindowHeight)
                    .AddGUI("Settings", ScreenSettings.GetData())
                    .AddGUI("MainMenu", Menu.GetData());
            Screen.BuildFrame();
            Log.Verbose($"Window Refreshed: W:{Screen.width} H:{Screen.height}");
        }
    }
    #endregion

    #region Console Renderer
    // New Idea: Make some handles for other methods to access and modify the screen instead of going through a filter
    //           to select a renderer method
    private void RenderMethod(RendererType type, Screen screen)
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
                screen.RenderFrame();
                break;

            case RendererType.DVD:
                _isRefreshing = true;
                Log.Debug("DVD");
                screen.RenderFrame();
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

        while (true)
        {
            await Task.Run(async () =>
            {
                // Check Screen Resize
                RebuildScreen();
                Screen.ClearFrame();
                // Render
                RenderMethod(renderSelection, Screen);

                await CheckPause(); // await this or it'll lag like hell | refactor this command next time to properly use threading pause
            });

            if (_isExit) return;
        }
        // await Refresh(screen);
    }

    private async Task CheckPause()
    {
        if (Properties == null) return;

        if (_isRefreshing)
        {
            Thread.Sleep(rendererTimings.FrameRate);
        }
        else
        {
            // Pause block: Pauses until _isRefreshingPaused is unlatched)
            await Task.Run(() =>
            {
                while (_isRefreshingPaused)
                {
                    Thread.Sleep(rendererTimings.TickSpeed);
                }
            });
        }
    }

    private async Task KeyListener()
    {
        if (Properties == null) return;

        await Task.Run(() =>
        {
            while (true)
            {
                if (renderSelection == RendererType.SETTINGS)
                {
                    Thread.Sleep(rendererTimings.TickSpeed);
                    if (_isExit) return;
                }
                else
                {
                    ReadKey();
                    _isRefreshingPaused = false;
                    CheckKeys();

                    Log.Verbose($"Key: {_keyInfo.Key}");
                    Thread.Sleep(rendererTimings.TickSpeed);
                    if (_isExit) return;
                }
            }
        });
    }

    private void ReadKey()
    {
        _keyInfo = Console.ReadKey(true);
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
    #endregion

    public void Settings()
    {
        string err = "";
        if (Properties == null) return;

        while (renderSelection == RendererType.SETTINGS)
        {
            Screen.ClearFrame();
            Console.WriteLine(err);
            Console.WriteLine("[ Settings ]\n");
            Console.WriteLine("1  -  Exit Settings");
            Console.WriteLine($"2  -  Change Target FPS ({Properties.Refresh})");
            Console.WriteLine($"3  -  Change Background Tick Rate ({Properties.Tick})\n");
            Console.WriteLine($"4  -  Toggle Debug Logging ({Properties.IsDebug})");
            Console.WriteLine($"5  -  Toggle Verbose Logging ({Properties.IsVerbose})\n");
            Console.WriteLine($"Console/Terminal Rendering Engine {Properties.Release} by SlamTheDragon\n");
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
                    int test1 = Int32.Parse(SetProperty("Enter New Target FPS:"));
                    if (test1 <= 0)
                    {
                        err = $"Error: Value cannot be set below 1. Set: ({test1})";
                    }
                    else
                    {
                        Properties.Refresh = test1;
                    }
                    break;

                case "3":

                    int test2 = Int32.Parse(SetProperty("Enter New Tick Rate (Maximum 20):"));
                    if (test2 > 20 || test2 < 1)
                    {
                        err = $"Error: Value out of range. Set: ({test2})";
                    }
                    else
                    {
                        Properties.Tick = test2;
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
        rendererTimings.Refresh();
    }

    public string SetProperty(string description)
    {
        bool isNull = false;

        while (renderSelection == RendererType.SETTINGS)
        {
            Screen.ClearFrame();
            if (isNull) { Console.WriteLine("Warning: Please enter a valid value."); }
            else { Console.WriteLine(""); }

            Console.WriteLine($"{description} \n");

            var _ = Console.ReadLine();

            if (_ == null || _ == "" || !Regex.IsMatch(_, @"^\d+$"))
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