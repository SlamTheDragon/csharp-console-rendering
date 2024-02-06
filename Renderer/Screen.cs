using System.Text;

namespace RenderEngine;

internal class Screen
{
    /*****************************************************
        SCREEN PROPERTIES
    *****************************************************/
    public int width;
    public int height;

    public float[,] ScreenMappings { get; set; }
    private char[,] GUIMappings { get; set; }
    private string[,] RenderedFrame { get; set; }
    private Dictionary<string, List<List<object>>> ScreenGUI { get; set; } = [];


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

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                // initiate initial float values
                ScreenMappings[i, j] = Step();
            }
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < w; j++)
            {
                // initiate initial float values
                GUIMappings[i, j] = ' ';
            }
        }
    }

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

    public Screen AddGUI(string GUIName, List<List<object>> lists)
    {
        ScreenGUI.Add(GUIName, new List<List<object>>(lists));
        return this;
    }

    /*****************************************************
        METHODS
    *****************************************************/
    public void BuildFrame(string? SelectGUI = "MainMenu")
    {
        InterfaceDecoder(SelectGUI);

        // col | height
        for (int i = 0; i < height; i++)
        {
            // row | width
            for (int j = 0; j < width; j++)
            {
                RenderedFrame[i, j] = GetPixelBrightness(ScreenMappings[i, j]) + InterfaceRenderer(j*2, i) + "\x1b[0m";
            }
        }
    }

    private string InterfaceRenderer(int x, int y)
    {
        char a = GUIMappings[y, x];
        char b = GUIMappings[y, x + 1];
        return $"{a}{b}";
    }

    private void InterfaceDecoder(string? SelectGUI)
    {
        if (SelectGUI == null) return;

        char[] text = [];
        int[] coordinates = [0, 0];
        int border = 0;

        foreach (var row in ScreenGUI[SelectGUI])
        {
            switch (row[0])
            {
                case InterfaceTypes.TEXT:
                    text = ((string)row[1]).ToCharArray();
                    coordinates = (int[])row[2];
                    break;

                case InterfaceTypes.BUTTON:
                    text = ((string)row[1]).ToCharArray();
                    coordinates = (int[])row[2];
                    border = (int)row[3];
                    break;

                default:
                    break;
            }

            InterfaceBuilder(text, coordinates, border);
        }
    }

    private void InterfaceBuilder(char[] text, int[] coordinates, int? border = 0)
    {
        int x = coordinates[0];
        int y = coordinates[1];

        for (int i = 0; x < text.Length; i++, x++)
        {
            GUIMappings[y, x] = text[i];
        }

        // reset
        x -= text.Length;

        if (border == 0) return;
        for (int i = 0; x < (int)Math.Floor((decimal)text.Length/2); i++, x++)
        {
            ScreenMappings[y, x] = 0.50f;
        }
    }

    private static string GetPixelBrightness(float f)
    {
        byte step = (byte)Math.Floor(f / 1.0 * 255);
        byte textColor = 97;
        if (step > 128)
        {
            textColor = 30;
        }

        string brightness = $"\x1b[48;2;{step};{step};{step}m\x1b[{textColor}m";
        return brightness;
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
            {
                frame.Append(RenderedFrame[i, j]);
            }
        }
        Console.WriteLine(frame.ToString());
    }
    public static void ClearFrame()
    {
        // this just doesnt clear all the buffers so ya
        Console.Clear();

        // idk any differences:
        // Console.Write("\u001b[2J");

        // Might not be supported on other console windows:
        Console.Write("\x1b[3J");
    }
}

internal enum InterfaceTypes
{
    TEXT,
    BUTTON,
}

internal sealed class ScreenInterfaceBuilder
{
    private List<List<object>> ScreenGUI { get; set; }

    public ScreenInterfaceBuilder()
    {
        ScreenGUI = [];
    }

    public ScreenInterfaceBuilder AddText(string text, int x, int y)
    {
        ScreenGUI.Add([InterfaceTypes.TEXT, text, new int[] { x, y }]);
        return this;
    }

    public ScreenInterfaceBuilder AddButton(string text, int x, int y, int border = 0)
    {
        ScreenGUI.Add([InterfaceTypes.BUTTON, text, new int[] { x, y }, border]);
        return this;
    }

    public List<List<object>> GetData()
    {
        return ScreenGUI;
    }
}