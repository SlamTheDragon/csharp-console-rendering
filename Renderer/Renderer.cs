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
    private ConsoleKeyInfo _keyInfo;

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
    private ScreenInterfaceBuilder Menu { get; set; }

    private readonly ManualResetEventSlim _rendererPauseEvent = new(true);
    private enum RendererType
    {
        DEFAULT,
    }
    private enum RendererOverlay
    {
        NONE,
        DEBUG,
        DETAILED_DEBUG,
        HELP,
        SETTINGS,
        MENU
    }

    private volatile RendererType _renderSelection = RendererType.DEFAULT;
    private volatile RendererOverlay _overlay = RendererOverlay.MENU;

    private volatile bool _isExit = false;
    private volatile bool _isAutoRefresh = false;
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
                    .AddDynamicText("FIXME: DYNAMIC ERROR DESCRIPTOR", 0, 0)
                    .AddText("[ Settings ]", 0, 1)
                    .AddText($"Console/Terminal Rendering Engine {Data.Properties.Release} by SlamTheDragon", 0, 9)
                    .AddText("[ Keys ]", 0, 11)
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
                    .AddButton(" 1  -  Exit Settings", 0, 2, Foo1, 1)
                    .AddButton($" 2  -  Change Target FPS ({Data.Properties.Refresh})", 0, 3, Foo2, 1)
                    .AddButton($" 3  -  Change Background Tick Rate ({Data.Properties.Tick})", 0, 4, Foo3, 1)
                    .AddButton($" 4  -  Toggle Debug Logging ({Data.Properties.IsDebug})", 0, 5, Foo4, 1)
                    .AddButton($" 5  -  Toggle Verbose Logging ({Data.Properties.IsVerbose})", 0, 6, Foo5, 1)
                    .AddTextInput(Foo6, 0, 7, 1);
        Debug = new ScreenInterfaceBuilder()
                    .AddText("Sample Text Debug", 0, 0);
        DetailedDebug = new ScreenInterfaceBuilder()
                    .AddText("Sample Text Detailed Debug", 0, 0);
        Help = new ScreenInterfaceBuilder()
                    .AddText("Sample Text Help", 0, 0);
        Menu = new ScreenInterfaceBuilder()
                    .AddText("Menu", 0, 0)
                    .AddButton(" 1 - Start Sandbox  ", 0, 1, () => { Log.Debug("Function Not Yet Implemented");}, 1)
                    .AddButton(" 2 - Screen Test    ", 0, 2, () => { Log.Debug("Function Not Yet Implemented");}, 1)
                    .AddButton(" 3 - Pointer Test   ", 0, 3, () => { Log.Debug("Function Not Yet Implemented");}, 1)
                    .AddButton(" 4 - 3D Engine Test ", 0, 4, () => { Log.Debug("Function Not Yet Implemented");}, 1)
                    .AddButton(" 5 - Save & Exit    ", 0, 6, () => { _isExit = true; _rendererPauseEvent.Set(); }, 1)
                    ;

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

    // temporary
    public void Foo1()
    { Log.Info("Button Pressed 0");}
    public void Foo2()
    { Log.Info("Button Pressed 1");}
    public void Foo3()
    { Log.Info("Button Pressed 2");}
    public void Foo4()
    { Log.Info("Button Pressed 3");}
    public void Foo5()
    { Log.Info("Button Pressed 4");}
    public void Foo6()
    { Log.Info("Button Pressed 5");}

    private async Task BuildScreen(string overlayUI = "Menu")
    {
        // TODO: split these into individual methods as marked by a flag on which Overlay UI to build
        Screen = new Screen(Console.WindowWidth, Console.WindowHeight)
            .AddOverlay("Default", Default.GetList())
            .AddOverlay("Settings", ScreenSettings.GetList(), true)
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

                if (_isAutoRefresh)
                { Thread.Sleep(Timings.FrameRate); }
                else
                { Thread.Sleep(Timings.TickSpeed); _rendererPauseEvent.Wait(); }
            });

            if (_isExit) return;
        }
    }

    private async Task WindowSizeRefresh()
    {
        // FIXME: double check refresh if it's true
        while (true)
        {
            await Task.Run(async () =>
            {
                double baseWidth = (double)Console.WindowWidth / 2;
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
        await Task.Run(() =>
        {
            while (true)
            {
                _keyInfo = Console.ReadKey(true);
                _rendererPauseEvent.Set(); // resume renderer

                CheckKeys();
                Log.Debug($"Key: {_keyInfo.Key}");
                Thread.Sleep(Timings.TickSpeed);

                if (_isExit) break;
            }
        });
    }

    private void CheckKeys()
    {
        switch (_keyInfo.Key)
        {
            case ConsoleKey.Escape:
                // if (_overlay == RendererOverlay.MENU) { _isExit = true; return; }
                // _overlay = RendererOverlay.MENU;
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
                Screen.NextAction();
                break;

            case ConsoleKey.D2:
                break;

            case ConsoleKey.D3:
                break;

            case ConsoleKey.D4:
                break;

            case ConsoleKey.D5:
                break;

            case ConsoleKey.D6:
                break;

            case ConsoleKey.D7:
                break;

            case ConsoleKey.D8:
                break;

            case ConsoleKey.D9:
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

            default:
                break;
        }
    }

    // public void Settings() // you have to rebuild the GUI somewhere.
    //                        // We could still try and implement this kind of functionality while still
    //                        // following the rendering rules of the Screen class
    // {
    //     string err = "";

    //     while (_renderSelection == RendererType.SETTINGS)
    //     {
    //         // reset var
    //         err = "";

    //         string input = Console.ReadLine();

    //         switch (input)
    //         {
    //             case "1":
    //                 _renderSelection = RendererType.DEFAULT;
    //                 _rendererPauseEvent.Set();
    //                 break;

    //             case "2":
    //                 int test1 = Int32.Parse(SetProperty("Enter New Target FPS:"));
    //                 if (test1 <= 0)
    //                 {
    //                     err = $"Error: Value cannot be set below 1. Set: ({test1})";
    //                 }
    //                 else
    //                 {
    //                     Data.Properties.Refresh = test1;
    //                 }
    //                 break;

    //             case "3":

    //                 int test2 = Int32.Parse(SetProperty("Enter New Tick Rate (Maximum 20):"));
    //                 if (test2 > 20 || test2 < 1)
    //                 {
    //                     err = $"Error: Value out of range. Set: ({test2})";
    //                 }
    //                 else
    //                 {
    //                     Data.Properties.Tick = test2;
    //                 }
    //                 break;

    //             case "4":
    //                 Data.Properties.IsDebug = !Data.Properties.IsDebug;
    //                 err = "Info: Restart Required";
    //                 break;

    //             case "5":
    //                 Data.Properties.IsVerbose = !Data.Properties.IsVerbose;
    //                 err = "Info: Restart Required";
    //                 break;

    //             default:
    //                 err = "Error: No cases matched.";
    //                 break;
    //         }

    //         Data.Refresh();
    //         Timings.Refresh(Data);
    //     }
    // }

    // public string SetProperty(string description)
    // {
    //     bool isNull = false;

    //     while (_renderSelection == RendererType.SETTINGS)
    //     {
    //         Screen.ClearFrame();
    //         if (isNull) { Console.WriteLine("Warning: Please enter a valid value."); }
    //         else { Console.WriteLine(""); }

    //         Console.WriteLine($"{description} \n");

    //         var _ = Console.ReadLine();

    //         if (_ == null || _ == "" || !Regex.IsMatch(_, @"^\d+$"))
    //         { isNull = true; }
    //         else
    //         { return _; }
    //     }

    //     return "";
    // }
}