using System.Text;
using System.Text.Json;
using System.Reflection;

namespace Utilities;

public class Configure
{
    /// <summary>
    /// Gets or sets the properties.
    /// </summary>
    public static Properties Properties { get; set; } = new();

    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    /// <summary>
    /// Initializes a new instance of the <see cref="Configure"/> class.
    /// </summary>
    protected Configure()
    {
        Properties configProperties = new();

        string defaultJson = JsonSerializer.Serialize(configProperties, _options);

        // Create default config file if it doesn't exist
        if (!File.Exists(_configPath))
        {
            using FileStream fileStream = File.Create(_configPath);
            // ->
            byte[] contentBytes = Encoding.UTF8.GetBytes(defaultJson);
            fileStream.Write(contentBytes);
        }   // then

        // Obtain configuration from file
        var file = File.ReadAllText(_configPath);
        Properties = JsonSerializer.Deserialize<Properties>(file);
    }

    /// <summary>
    /// Saves the properties to the config file.
    /// </summary>
    public static void Save()
    {
        // Serialize Latest Configs to Json
        string json = JsonSerializer.Serialize(Properties, _options);

        // Obtain old Config from file
        string oldFile = File.ReadAllText(_configPath);

        // Overwrite & Append
        string newFile = oldFile.Replace(oldFile, json);
        File.WriteAllText(_configPath, newFile);
    }
}

/// <summary>
/// Program properties
/// </summary>
public sealed class Properties
{
    /// <summary>
    /// Config Json Version
    /// </summary>
    public string Version { get; set; } = "1";
    /// <summary>
    /// Release Version
    /// </summary>
    public string Release { get; set; }
    /// <summary>
    /// Terminal Screen Refresh Speed
    /// </summary>
    public int Refresh { get; set; } = 5;
    /// <summary>
    /// Terminal Task Tick Speed (20 ticks = 1s)
    /// </summary>
    public int Tick { get; set; } = 1;
    /// <summary>
    /// A flag to enable file logging of debug
    /// </summary>
    public bool IsDebug { get; set; } = false;
    /// <summary>
    /// A flag to enable file logging of verbose
    /// </summary>
    public bool IsVerbose { get; set; } = false;

    public Properties()
    {
        // Get the assembly of the current executing code
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Get the assembly information
        AssemblyName assemblyName = assembly.GetName();
        if (assemblyName.Version == null) Release = "unknown";
        else Release = assemblyName.Version.ToString();
    }
}
