using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Utilities;

public class Configure
{
    public static Properties? Properties { get; private set; }
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly bool _configLock = false;
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    protected Configure()
    {
        if (_configLock) return;
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
        _configLock = true;
    }

    public void Save() {
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
    /// Json Version
    /// </summary>
    public string Version { get; set; } = "1";
    /// <summary>
    /// Release Version
    /// </summary>
    public string Release { get; set; }
    /// <summary>
    /// Console Screen Refresh Speed
    /// </summary>
    public int Refresh { get; set; } = 5;
    /// <summary>
    /// Console Task Tick Speed (20 ticks = 1s)
    /// </summary>
    public int Tick { get; set; } = 2;
    /// <summary>
    /// A switch to enable file logging of debug
    /// </summary>
    public bool IsDebug { get; set; } = false;
    /// <summary>
    /// A switch to enable file logging of verbose
    /// </summary>
    public bool IsVerbose { get; set; } = false;

    public Properties()
    {
        // Get the assembly of the current executing code
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Get the assembly information
        AssemblyName assemblyName = assembly.GetName();

        if (assemblyName.Version == null) return;

        // Access the version information
        Release = assemblyName.Version.ToString();
    }
}
