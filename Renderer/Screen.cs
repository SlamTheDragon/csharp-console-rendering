using Utilities;
using System.Text;

namespace RenderEngine;
internal class Screen
{
    #region Properties
    /*****************************************************
        SCREEN PROPERTIES
    *****************************************************/
    public int width;
    public int height;

    public float[,] ScreenMappings { get; set; }
    private char[,] GUIMappings { get; set; }
    private string[,] RenderedFrame { get; set; } // can be char but would need it to be three dimensional

    private Dictionary<string, List<List<object>>> ScreenOverlayUI { get; set; } = [];
    private Dictionary<string, bool> OverlayLockables { get; set; } = [];
    private string SelectedOverlay { get; set; }

    private List<Action> ActionStore { get; set; } = [];
    private byte ActionIndex { get; set; }
    private List<char> KeyStored { get; set; } = [];
    private Logger Log { get; set; }
    #endregion

    #region Constructor
    /*****************************************************
        CONSTRUCTOR
    *****************************************************/
    public Screen(int w, int h)
    {
        // Divide by two since a "pixel" in this context occupies two columns 
        // to create a visual "square" in the console
        decimal baseWidth = (decimal)w / 2;

        width = (int)Math.Floor(baseWidth);
        height = h - 1;

        ScreenMappings = new float[height, width];
        GUIMappings = new char[height, w]; // since we're dealing with texts here
        RenderedFrame = new string[height, width];

        // how to combine these two for loops lol
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            { ScreenMappings[i, j] = Step(); }
        }
    }

    // FIXME: Temporary
    private float silliness = 0.001f;
    private float Step()
    {
        silliness += 0.001f;
        if (silliness == 1.0)
        {
            silliness = 0.001f;
        }
        return silliness;
    }

    public Screen AddOverlay(string Overlay, List<List<object>> lists, bool isLockable = false)
    {
        ScreenOverlayUI.Add(Overlay, new List<List<object>>(lists));
        OverlayLockables.Add(Overlay, isLockable);
        return this;
    }

    public Screen AddLogger(Logger logger)
    {
        Log = logger;
        return this;
    }
    #endregion

    /*****************************************************
        METHODS
    *****************************************************/

    /// <summary>
    /// Screen Frame Builder
    /// </summary>
    /// <param name="SelectOverlay"></param>
    public async Task BuildFrame(string SelectOverlay)
    {
        SelectedOverlay = SelectOverlay;
        ActionIndex = 0;

        // clear gui - the cheap solution but unoptimized
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < (width * 2); j++)
            { GUIMappings[i, j] = '\u00A0'; }
        }

        await InterfaceDecoder(SelectOverlay);

        // TODO: await - Call a method to calculate results for ScreenMappings (not unless it's still not done from doing)

        // col | height
        for (int i = 0; i < height; i++)
        {
            // row | width
            for (int j = 0; j < width; j++)
            {
                RenderedFrame[i, j] =
                GetPixelBrightness(ScreenMappings[i, j])
                 + InterfaceRenderer(j * 2, i)
                 + "\x1b[0m";
            }
        }
    }

    private static string GetPixelBrightness(float f)
    {
        byte step = (byte)Math.Floor(f / 1.0 * 255);
        byte textColor = step > 128 ? (byte)30 : (byte)97; // Invert text color based on brightness

        string brightness = $"\x1b[48;2;{step};{step};{step}m\x1b[{textColor}m";
        return brightness;
    }

    /// <summary>
    /// Insert UI Components to the Screen
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private string InterfaceRenderer(int x, int y)
    {
        char a = GUIMappings[y, x];
        char b = GUIMappings[y, x + 1];
        return $"{a}{b}";
    }

    /// <summary>
    /// Decode the Interface from the ScreenOverlayUI
    /// </summary>
    /// <param name="SelectOverlay"></param>
    /// <returns></returns>
    private async Task InterfaceDecoder(string SelectOverlay)
    {
        char[] text;
        int[] coords;
        int border;

        foreach (var row in ScreenOverlayUI[SelectOverlay])
        {
            // Assign
            switch (row[0])
            {
                case InterfaceTypes.TEXT:
                    text = ((string)row[1]).ToCharArray();
                    coords = (int[])row[2];

                    await InterfaceBuilder(text, coords);
                    break;

                case InterfaceTypes.MAGNETIC_TEXT:
                    text = ((string)row[1]).ToCharArray();

                    if ((AlignText)row[2] == AlignText.alignLeft)
                    {
                        coords = [0, (int)row[3]];
                        await InterfaceBuilder(text, coords);
                    }
                    if ((AlignText)row[2] == AlignText.alignCenter)
                    {
                        int textLength = text.Length;
                        int x = (width - textLength) / 2;
                        coords = [x, (int)row[3]];
                        await InterfaceBuilder(text, coords);
                    }
                    if ((AlignText)row[2] == AlignText.alignRight)
                    {
                        int textLength = text.Length;
                        int x = width - textLength;
                        coords = [x, (int)row[3]];
                        await InterfaceBuilder(text, coords);
                    }
                    break;

                case InterfaceTypes.DYNAMIC_TEXT:
                    text = ((Func<string>)row[1])().ToCharArray();
                    coords = (int[])row[2];

                    await InterfaceBuilder(text, coords);
                    break;

                case InterfaceTypes.BUTTON:
                    text = ((string)row[1]).ToCharArray();
                    coords = (int[])row[2];

                    ActionStore.Add((Action)row[3]);

                    border = (int)row[4];

                    await InterfaceBuilder(text, coords, border);
                    break;

                case InterfaceTypes.DYNAMIC_BUTTON:
                    text = ((Func<string>)row[1])().ToCharArray();
                    coords = (int[])row[2];

                    ActionStore.Add((Action)row[3]);

                    border = (int)row[4];
                    await InterfaceBuilder(text, coords, border);
                    break;

                case InterfaceTypes.SLIDER:
                    // await InterfaceBuilder(text, coords, border);
                    break;

                case InterfaceTypes.TEXT_INPUT:
                    text = ((Func<char[]>)row[1])();

                    coords = (int[])row[2];
                    border = (int)row[3];
                    await InterfaceBuilder(text, coords, border);
                    break;
            }
        }
    }

    /// <summary>
    /// Sub method for InterfaceDecoder
    /// </summary>
    /// <param name="text"></param>
    /// <param name="coords"></param>
    /// <param name="border"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private async Task InterfaceBuilder(char[] text, int[] coords, int border = 0)
    {
        int x = coords[0];
        int y = coords[1];
        int targetX = border >= 2 ? x + (border - 1) * 2 : x;
        int targetY = border >= 2 ? y + (border - 1) : y;
        bool isOutOfBounds = false;

        if (y > height) return;

        for (int i = 0; i < text.Length; i++, targetX++)
        {
            if (targetY < GUIMappings.GetLength(0) && targetX < GUIMappings.GetLength(1))
            { GUIMappings[targetY, targetX] = text[i]; }
            else
            { isOutOfBounds = true; }
        }

        // reset "pointer"
        x = coords[0];

        // Map Out Background/Borders FIXME: move out the inline math operations
        if (border == 1)
        {
            int newX = (int)Math.Floor((decimal)x / 2);
            for (int i = 0; i <= (int)Math.Floor((decimal)text.Length / 2); i++, newX++)
            {
                if (newX < ScreenMappings.GetLength(1))
                { ScreenMappings[y, newX] = 0.30f; }
                else
                { isOutOfBounds = true; }
            }
        }
        else if (border > 1)
        {
            // FIXME: this thing breaks with values above 2, your formula isnt right lol
            int newY = y;

            for (int i = 0; i <= targetY; i++, newY++)
            {
                int newX = (int)Math.Floor((decimal)x / 2);
                for (int j = 0; j <= ((int)Math.Floor((decimal)text.Length / 2) + border); j++, newX++)
                {
                    if (newY < ScreenMappings.GetLength(0) && newX < ScreenMappings.GetLength(1))
                    { ScreenMappings[newY, newX] = 0.30f; }
                    else
                    { isOutOfBounds = true; }
                }
            }
        }

        // Define the Out of Bounds Exception
        if (isOutOfBounds)
        {
            try
            { throw new IndexOutOfRangeException("A UI Component is out of bounds."); }
            catch (Exception e)
            { await HandleError(e); }
        }
    }

    public void RenderFrame()
    {
        bool newLineLock = false;
        StringBuilder frame = new();

        // col
        for (int i = 0; i < height; i++)
        {
            if (newLineLock) frame.Append('\n');
            else newLineLock = true;

            // row
            for (int j = 0; j < width; j++)
            { frame.Append(RenderedFrame[i, j]); }
        }

        Console.Write(frame.ToString());
    }

    public void NextKey(char keyInfo)
    { KeyStored.Add(keyInfo); }

    public void NextActionNum1()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum2()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum3()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum4()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum5()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum6()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum7()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum8()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionNum9()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionKeyW()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionKeyA()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionKeyS()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionKeyD()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionSpacebar()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionBackspace()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionUp()
    {
        if (OverlayLockables[SelectedOverlay])
        {
            if (ActionIndex == 0) Log.Debug($"Selection Index: {ActionIndex}");
            else { ActionIndex--; Log.Debug($"Selection Index: {ActionIndex}"); }
            // link methods needed to render the indicated UI change into an action list to queue up for the next
            // build frame call
        }
        else
        { Log.Debug("Info: No Action Available"); }
    }

    public void NextActionDown()
    {
        if (OverlayLockables[SelectedOverlay])
        {
            if (ActionIndex == (ActionStore.Count - 1)) Log.Debug($"Selection Index: {ActionIndex}");
            else { ActionIndex++; Log.Debug($"Selection Index: {ActionIndex}"); }
            // link methods needed to render the indicated UI change into an action list to queue up for the next
            // build frame call
        }
        else
        { Log.Debug("Info: No Action Available"); }
    }

    public void NextActionLeft()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionRight()
    {
        // Not Yet Implemented
        Log.Debug("Info: No Action Available");
    }

    public void NextActionEnter()
    {
        // Execute available action
        if (OverlayLockables[SelectedOverlay])
        {
            ActionStore[ActionIndex]();
        }
        else
        { Log.Debug("Info: No Action Available"); }
    }



    // Cleanup section
    public static void ClearFrame()
    {
        Console.Clear();
        Console.Write("\x1b[3J"); // alt
    }

    /// <summary>
    /// Handle Errors with two different levels
    /// </summary>
    /// <param name="e"></param>
    /// <param name="isMajor"></param>
    /// <returns></returns>
    private async Task HandleError(Exception e, bool isMajor = false)
    {
        if (Log == null) return;

        await Task.Run(() =>
        {
            if (isMajor) Log.Warn(e.Message);
            else Log.Info(e.Message);

            if (e.StackTrace == null) return;
            Log.Verbose(e);
        });
    }
}

internal enum InterfaceTypes
{
    TEXT,
    DYNAMIC_TEXT,
    MAGNETIC_TEXT,
    BUTTON,
    DYNAMIC_BUTTON,
    SLIDER,
    TEXT_INPUT
}

internal enum AlignText
{
    alignLeft,
    alignRight,
    alignCenter
}

internal sealed class ScreenInterfaceBuilder
{
    /// <summary>
    /// Component GUI Container
    /// - Contains the type of component, and all essential data for rendering
    /// </summary>
    private List<List<object>> ScreenGUI { get; set; }

    public ScreenInterfaceBuilder()
    { ScreenGUI = []; }

    /// <summary>
    /// Add a Text to the final GUI
    /// </summary>
    /// <param name="text">The text to be added</param>
    /// <param name="x">The x-coordinate of the text</param>
    /// <param name="y">The y-coordinate of the text</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddText(string text, int x, int y)
    {
        ScreenGUI.Add([InterfaceTypes.TEXT, text, new int[] { x, y }]);
        return this;
    }
    /// <summary>
    /// Add a Text with alignment to the final GUI
    /// </summary>
    /// <param name="text">The text to be added</param>
    /// <param name="alignText">The alignment of the text (alignLeft, alignRight, alignCenter)</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddMagneticText(string text, AlignText alignText, int y)
    {
        ScreenGUI.Add([InterfaceTypes.TEXT, text, alignText, y]);
        return this;
    }
    /// <summary>
    /// Add a Dynamic Text to the final GUI
    /// - Dynamic Texts are texts that change over time
    /// </summary>
    /// <param name="textMethod">The method that generates the dynamic text</param>
    /// <param name="x">The x-coordinate of the text</param>
    /// <param name="y">The y-coordinate of the text</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddDynamicText(Func<string> textMethod, int x, int y)
    {
        ScreenGUI.Add([InterfaceTypes.DYNAMIC_TEXT, textMethod, new int[] { x, y }]);
        return this;
    }
    /// <summary>
    /// Add a Button to the final GUI
    /// </summary>
    /// <param name="text">The text of the button</param>
    /// <param name="x">The x-coordinate of the button</param>
    /// <param name="y">The y-coordinate of the button</param>
    /// <param name="onInput">The action to be executed when the button is pressed</param>
    /// <param name="border">The border size of the button</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddButton(string text, int x, int y, Action onInput, int border = 0)
    {
        ScreenGUI.Add([InterfaceTypes.BUTTON, text, new int[] { x, y }, onInput, border]);
        return this;
    }
    /// <summary>
    /// Add a Dynamic Button to the final GUI
    /// </summary>
    /// <param name="textMethod">The method that generates the dynamic text of the button</param>
    /// <param name="x">The x-coordinate of the button</param>
    /// <param name="y">The y-coordinate of the button</param>
    /// <param name="onInput">The action to be executed when the button is pressed</param>
    /// <param name="border">The border size of the button</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddDynamicButton(Func<string> textMethod, int x, int y, Action onInput, int border = 0)
    {
        ScreenGUI.Add([InterfaceTypes.DYNAMIC_BUTTON, textMethod, new int[] { x, y }, onInput, border]);
        return this;
    }
    /// <summary>
    /// Add a Slider control to the final GUI
    /// </summary>
    /// <param name="action">The action to be executed when the slider value changes</param>
    /// <param name="x">The x-coordinate of the slider</param>
    /// <param name="y">The y-coordinate of the slider</param>
    /// <param name="onInput">The action to be executed when the slider is interacted with</param>
    /// <param name="border">The border size of the slider</param>
    /// <returns>The ScreenInterfaceBuilder instance</returns>
    public ScreenInterfaceBuilder AddSlider(Action action, int x, int y, Action onInput, int border = 0)
    {
        ScreenGUI.Add([InterfaceTypes.SLIDER, action, new int[] { x, y }, onInput, border]);
        return this;
    }
    /// <summary>
    /// Add a User Text Input
    /// </summary>
    /// <param name="action"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="border"></param>
    /// <returns></returns>
    public ScreenInterfaceBuilder AddTextInput(Func<char[]> textMethod, int x, int y, int border = 0)
    {
        ScreenGUI.Add([InterfaceTypes.TEXT_INPUT, textMethod, new int[] { x, y }, border]);
        return this;
    }
    /// <summary>
    /// Export the GUI for rendering
    /// </summary>
    /// <returns>The GUI as a list of lists of objects</returns>
    public List<List<object>> GetList()
    { return ScreenGUI; }
}