using System.Text.RegularExpressions;
using Utilities;
using Utilities.Data;

namespace RenderEngine;
public class Renderer
{
    #region Renderer Properties
    /*****************************************************
        MAIN PROPERTIES
    *****************************************************/
    private Logger Log { get; set; }
    private SharedData Data { get; set; }
    private volatile bool _isExit = false;

    /*****************************************************
        SCREEN PROPERTIES
    *****************************************************/
    private Screen Screen { get; set; }
    private RendererTimings Timings { get; set; }
    private ScreenInterfaceBuilder Default { get; set; }
    private ScreenInterfaceBuilder Debug { get; set; }
    private ScreenInterfaceBuilder DetailedDebug { get; set; }
    private ScreenInterfaceBuilder Help { get; set; }
    private ScreenInterfaceBuilder ScreenSettings { get; set; }
    private ScreenInterfaceBuilder SubScreenSettings { get; set; }
    private ScreenInterfaceBuilder Menu { get; set; }

    private readonly ManualResetEventSlim _rendererPauseEvent = new(true);
    private enum RendererOverlay
    {
        NONE,
        DEBUG,
        DETAILED_DEBUG,
        HELP,
        SETTINGS,
        SUB_SETTINGS,
        MENU
    }

    private volatile RendererOverlay _overlay = RendererOverlay.MENU;
    private List<char> StoreKeys { get; set; } = [];
    #endregion

    #region Constructor
    public Renderer AddLogger(Logger logInstance)
    {
        Log = logInstance;
        return this;
    }
    public Renderer AddData(SharedData data)
    {
        Data = data;
        return this;
    }
    public Renderer AddTimingsManager(RendererTimings timings)
    {
        Timings = timings;
        return this;
    }
    #endregion

    #region Screen Builder
    // convert to constructor?
    public async Task Initialize()
    {
        Log.Info("Starting Console Game Engine...", true);
        Log.Info("> Building Screen", true);

        // Build GUI
        Default = new ScreenInterfaceBuilder()
                    .AddText("Press ESC to Open Menu. Press F2 to Configure.", 0, 0);
        ScreenSettings = new ScreenInterfaceBuilder()
                    .AddText("[ SETTINGS ]", 0, 1)
                    .AddDynamicText(() => { return $"Console/Terminal Rendering Engine v{Data.Properties.Release} by SlamTheDragon"; }, 0, 9)
                    .AddText("[ KEYS ]", 0, 11)
                    .AddText("ESC       - Open Menu", 0, 12)
                    .AddText("F1        - Toggle Minimal Debug Overlay", 0, 13)
                    .AddText("F2        - Open This Menu", 0, 14)
                    .AddText("F3        - Toggle Extensive Debug Overlay", 0, 15)
                    .AddText("F4        - Toggle Help/Cheat Sheet", 0, 16)
                    .AddText("Arrows    - Look Up/Down/Left/Right | Navigate on menu's", 0, 17)
                    .AddText("WASD      - Move Forward/Backward/Left/Right", 0, 18)
                    .AddText("Space     - Fly Up", 0, 19)
                    .AddText("Shift     - Fly Down", 0, 20)
                    .AddText("Enter     - Confirm Selection/Interact", 0, 21)
                    .AddText("Backspace - Confirm Selection/Interact", 0, 22)
                    .AddDynamicButton(() => { return $" 1  -  Change Target FPS ({Data.Properties.Refresh})        "; }, 0, 2, async () =>
                    {
                        _overlay = RendererOverlay.SUB_SETTINGS;
                    }, 1)
                    .AddDynamicButton(() => { return $" 3  -  Toggle Debug Logging ({Data.Properties.IsDebug})     "; }, 0, 3, async () =>
                    {
                        Data.Properties.IsDebug = !Data.Properties.IsDebug;
                        Data.Refresh();
                        Timings.Refresh(Data);

                        await BuildScreen(overlay);
                        _rendererPauseEvent.Set();
                    }, 1)
                    .AddDynamicButton(() => { return $" 4  -  Toggle Verbose Logging ({Data.Properties.IsVerbose}) "; }, 0, 4, async () =>
                    {
                        Data.Properties.IsVerbose = !Data.Properties.IsVerbose;
                        Data.Refresh();
                        Timings.Refresh(Data);

                        await BuildScreen(overlay);
                        _rendererPauseEvent.Set();
                    }, 1);
        SubScreenSettings = new ScreenInterfaceBuilder()
                    .AddText("[ SETTINGS ]", 0, 1)
                    .AddText("Enter New FPS Target:", 0, 2)
                    .AddTextInput(StoreKeys.ToArray, 0, 3, 1);
        Debug = new ScreenInterfaceBuilder()
                    .AddText("Sample Text Debug", 0, 0);
        DetailedDebug = new ScreenInterfaceBuilder()
                    .AddText("Sample Text Detailed Debug", 0, 0);
        Help = new ScreenInterfaceBuilder()
                    .AddText("[ SHORTCUT HELP ]", 0, 1)
                    .AddText("ESC       - Open Menu", 0, 2)
                    .AddText("F1        - Toggle Minimal Debug Overlay", 0, 3)
                    .AddText("F2        - Open Settings", 0, 4)
                    .AddText("F3        - Toggle Extensive Debug Overlay", 0, 5)
                    .AddText("F4        - Toggle This Menu", 0, 6)
                    .AddText("Arrows    - Look Up/Down/Left/Right | Navigate on menu's", 0, 7)
                    .AddText("WASD      - Move Forward/Backward/Left/Right", 0, 8)
                    .AddText("Space     - Fly Up", 0, 9)
                    .AddText("Shift     - Fly Down", 0, 10)
                    .AddText("Enter     - Confirm Selection/Interact", 0, 11)
                    .AddText("Backspace - Confirm Selection/Interact", 0, 12);
        Menu = new ScreenInterfaceBuilder()
                    .AddText("Press ESC to close this menu, use ARROW KEYS to navigate", 0, 0)
                    .AddText("[ MENU ]", 0, 1)
                    .AddButton(" 1 - Start Sandbox Engine ", 0, 2, () => { Log.Debug("Function Not Yet Implemented"); }, 1)
                    .AddButton(" 2 - Screen Test          ", 0, 3, () => { Log.Debug("Function Not Yet Implemented"); }, 1)
                    .AddButton(" 3 - Pointer Test         ", 0, 4, () => { Log.Debug("Function Not Yet Implemented"); }, 1)
                    .AddButton(" 4 - 3D Engine Test       ", 0, 5, () => { Log.Debug("Function Not Yet Implemented"); }, 1)
                    .AddButton(" 5 - Save & Exit          ", 0, 7, () => { _isExit = true; _rendererPauseEvent.Set(); }, 1);

        // Build Screen
        await BuildScreen();

        Log.Info("    > Initializing Renderer Methods", true);

        // ...

        Log.Info("> Starting...", true);
        Screen.ClearFrame();

        Task window = Task.Run(Refresh);
        Task windowRefresher = Task.Run(WindowSizeRefresh);
        Task keyboard = Task.Run(KeyListener);
        Log.Info("Application Ready");

        await Task.WhenAll(window, windowRefresher, keyboard);
    }

    private async Task BuildScreen(string overlayUI = "Menu")
    {
        // TODO: split these into individual methods as marked by a flag on which Overlay UI to build
        Screen = new Screen(Console.WindowWidth, Console.WindowHeight)
            .AddOverlay("Default", Default.GetList())
            .AddOverlay("Settings", ScreenSettings.GetList(), true)
            .AddOverlay("SubSettings", SubScreenSettings.GetList(), true)
            .AddOverlay("Debug", Debug.GetList())
            .AddOverlay("DetailedDebug", DetailedDebug.GetList())
            .AddOverlay("Help", Help.GetList())
            .AddOverlay("Menu", Menu.GetList(), true)
            .AddLogger(Log);

        await Screen.BuildFrame(overlayUI);
    }

    private string overlay = "Menu";

    private async Task RebuildScreen()
    {
        _rendererPauseEvent.Reset(); // pause renderer

        double baseWidth = (double)Console.WindowWidth / 2;
        int baseHeight = Console.WindowHeight - 1;

        string overlayCompare = "Menu";

        switch (_overlay)
        {
            case RendererOverlay.MENU:
                overlayCompare = "Menu";
                break;
            case RendererOverlay.DEBUG:
                overlayCompare = "Debug";
                break;
            case RendererOverlay.DETAILED_DEBUG:
                overlayCompare = "DetailedDebug";
                break;
            case RendererOverlay.HELP:
                overlayCompare = "Help";
                break;
            case RendererOverlay.SETTINGS:
                overlayCompare = "Settings";
                break;
            case RendererOverlay.SUB_SETTINGS:
                overlayCompare = "SubSettings";
                break;
            case RendererOverlay.NONE:
                overlayCompare = "Default";
                break;
        }

        if ((Screen.width != (int)Math.Floor(baseWidth)) || (Screen.height != baseHeight))
        {
            await BuildScreen(overlay);
            Log.Verbose($"Window Refreshed: W:{Screen.width} H:{Screen.height}");
        }
        else if (overlayCompare != overlay)
        {
            overlay = overlayCompare;
            await BuildScreen(overlay);
        }
    }
    #endregion

    #region Console Renderer
    // New Idea: Make some handles for other methods to access and modify the screen instead of going through a filter
    //           to select a renderer method
    private async Task Refresh()
    {
        while (true)
        {
            await Task.Run(async () =>
            {
                // Check Screen Resize or Changed
                await RebuildScreen();
                // Clear Buffer
                Screen.ClearFrame();
                // Render Screen
                Screen.RenderFrame();

                // if (_isAutoRefresh)
                // { Thread.Sleep(Timings.FrameRate); }
                // else
                // { Currently Unused } 
                Thread.Sleep(Timings.TickSpeed); _rendererPauseEvent.Wait();
            });

            if (_isExit) return;
        }
    }

    private async Task WindowSizeRefresh()
    {
        // FIXME: when the values are odd, the inner screen check condition is always true
        while (true)
        {
            await Task.Run(async () =>
            {
                int baseWidth = (int)Math.Floor((double)Console.WindowWidth / 2);
                int baseHeight = Console.WindowHeight - 1;

                if (Screen.width != baseWidth || Screen.height != baseHeight)
                { await RebuildScreen(); _rendererPauseEvent.Set(); }
            });
            Thread.Sleep(Timings.TickSpeed);
            if (_isExit) return;
        }
    }
    #endregion

    private async Task KeyListener()
    {
        await Task.Run(async () =>
        {
            while (true)
            {
                if (_overlay != RendererOverlay.SUB_SETTINGS)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    _rendererPauseEvent.Set();

                    CheckKeys(keyInfo);
                    Log.Debug($"Key: {keyInfo.Key}");
                    Thread.Sleep(Timings.TickSpeed);
                }
                else
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.Enter:
                            _overlay = RendererOverlay.SETTINGS;

                            await BuildScreen(overlay);
                            _rendererPauseEvent.Set();
                            break;

                        case ConsoleKey.Backspace:
                            if (StoreKeys.Count > 0)
                            { StoreKeys.RemoveAt(StoreKeys.Count - 1);}

                            await BuildScreen(overlay);
                            _rendererPauseEvent.Set();
                            break;

                        case ConsoleKey.Escape:
                            _overlay = RendererOverlay.SETTINGS;

                            await BuildScreen(overlay);
                            _rendererPauseEvent.Set();
                            break;

                        default:
                            StoreKeys.Add(keyInfo.KeyChar);

                            await BuildScreen(overlay);
                            _rendererPauseEvent.Set();
                            break;
                    }
                    Log.Debug($"Key handles has been redirected for another process");
                    Thread.Sleep(Timings.TickSpeed);
                }

                if (_isExit) break;
            }
        });
    }

    private void CheckKeys(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                _overlay = (_overlay == RendererOverlay.MENU)
                ? RendererOverlay.NONE
                : RendererOverlay.MENU;
                break;

            case ConsoleKey.F1:
                _overlay = (_overlay == RendererOverlay.DEBUG)
                ? RendererOverlay.NONE
                : RendererOverlay.DEBUG;
                break;

            case ConsoleKey.F2:
                _overlay = (_overlay == RendererOverlay.SETTINGS)
                ? RendererOverlay.NONE
                : RendererOverlay.SETTINGS;
                break;

            case ConsoleKey.F3:
                _overlay = (_overlay == RendererOverlay.DETAILED_DEBUG)
                ? RendererOverlay.NONE
                : RendererOverlay.DETAILED_DEBUG;
                break;

            case ConsoleKey.F4:
                _overlay = (_overlay == RendererOverlay.HELP)
                ? RendererOverlay.NONE
                : RendererOverlay.HELP;
                break;

            case ConsoleKey.D1:
                Screen.NextActionNum1();
                break;

            case ConsoleKey.D2:
                Screen.NextActionNum2();
                break;

            case ConsoleKey.D3:
                Screen.NextActionNum3();
                break;

            case ConsoleKey.D4:
                Screen.NextActionNum4();
                break;

            case ConsoleKey.D5:
                Screen.NextActionNum5();
                break;

            case ConsoleKey.D6:
                Screen.NextActionNum6();
                break;

            case ConsoleKey.D7:
                Screen.NextActionNum7();
                break;

            case ConsoleKey.D8:
                Screen.NextActionNum8();
                break;

            case ConsoleKey.D9:
                Screen.NextActionNum9();
                break;

            case ConsoleKey.W:
                Screen.NextActionKeyW();
                break;

            case ConsoleKey.A:
                Screen.NextActionKeyA();
                break;

            case ConsoleKey.S:
                Screen.NextActionKeyS();
                break;

            case ConsoleKey.D:
                Screen.NextActionKeyD();
                break;

            case ConsoleKey.Spacebar:
                Screen.NextActionSpacebar();
                break;

            case ConsoleKey.Backspace:
                Screen.NextActionBackspace();
                break;

            case ConsoleKey.UpArrow:
                Screen.NextActionUp();
                break;

            case ConsoleKey.RightArrow:
                Screen.NextActionRight();
                break;

            case ConsoleKey.LeftArrow:
                Screen.NextActionLeft();
                break;

            case ConsoleKey.DownArrow:
                Screen.NextActionDown();
                break;

            case ConsoleKey.Enter:
                Screen.NextActionEnter();
                break;
        }
    }
    private void ChangeTargetFPS()
    {
        // "Enter New Target FPS:"
        Data.Properties.Refresh = int.Parse(SetProperty());
    }

    public string SetProperty()
    {
        bool isNull = false;

        while (true)
        {
            if (isNull) { }
            else { }

            // Console.WriteLine($"{description} \n");

            var _ = Console.ReadLine();

            if (_ == null || _ == "" || !Regex.IsMatch(_, @"^\d+$"))
            { isNull = true; }
            else
            {
                Data.Refresh();
                Timings.Refresh(Data);
                return _;
            }
        }
    }
}