using Utilities.Data;

namespace Utilities;
public enum LogLevel
{
    INFO,
    WARN,
    ERROR,
    FATAL,
    DEBUG,
    VERBOSE
}

/// <summary>
/// Custom Logging Class
/// </summary>
public sealed class Logger
{
    #region Properties
    private string LogName { get; set; }
    private readonly bool _verbose;
    private readonly bool _debug;
    private readonly LogStreamer LogStreamer;
    private readonly SharedData Data;
    #endregion

    #region Constructor
    public Logger(string logName, LogStreamer streamWriter, SharedData data)
    {
        Data = data;
        LogName = logName;

        _verbose = Data.Properties.IsVerbose;
        _debug = Data.Properties.IsDebug;

        LogStreamer = streamWriter;
    }
    #endregion

    #region Logging Methods
    /// <summary>
    /// Log an INFO message
    /// </summary>
    /// <param name="input"></param>
    /// <param name="toWindow"></param>
    public void Info(dynamic input, bool toWindow = false)
    {
        LogStreamer.LogFormatter(LogLevel.INFO, input.ToString(), LogName);
        if (toWindow) { Console.WriteLine(input); }
    }
    /// <summary>
    /// Log a WARN message
    /// </summary>
    /// <param name="input"></param>
    public void Warn(dynamic input)
    {
        LogStreamer.LogFormatter(LogLevel.WARN, input.ToString(), LogName);
    }
    /// <summary>
    /// Log an ERROR message
    /// </summary>
    /// <param name="input"></param>
    /// <param name="toWindow"></param>
    public void Error(dynamic input, bool toWindow = false)
    {
        LogStreamer.LogFormatter(LogLevel.ERROR, input.ToString(), LogName);
        string exclusiveLogMessage = $"[{LogLevel.ERROR}] [{LogName.ToUpper()}] {input}";
        if (toWindow) { Console.WriteLine(exclusiveLogMessage); }
    }
    /// <summary>
    /// Log a FATAL message
    /// </summary>
    /// <param name="input"></param>
    /// <param name="toWindow"></param>
    public void Fatal(dynamic input, bool toWindow = false)
    {
        LogStreamer.LogFormatter(LogLevel.FATAL, input.ToString(), LogName);
        string exclusiveLogMessage = $"[{LogLevel.FATAL}] [{LogName.ToUpper()}] {input}";
        if (toWindow) { Console.WriteLine(exclusiveLogMessage); }
    }
    /// <summary>
    /// Log a DEBUG message
    /// </summary>
    /// <param name="input"></param>
    public void Debug(dynamic input)
    {
        if (!_debug) return;
        LogStreamer.LogFormatter(LogLevel.DEBUG, input.ToString(), LogName);
    }
    /// <summary>
    /// Log a VERBOSE message
    /// </summary>
    /// <param name="input"></param>
    public void Verbose(dynamic input)
    {
        if (!_verbose) return;
        LogStreamer.LogFormatter(LogLevel.VERBOSE, input.ToString(), LogName);
    }
    #endregion
}

public sealed class LogStreamer
{
    private readonly bool isLogFileCreated = false;
    private readonly string logPath;

    public LogStreamer()
    {
        // Delete the latest.log if it exist, otherwise just store the directory
        logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "latest.log");
        if (!Directory.Exists(logPath)) File.Delete(logPath);
        isLogFileCreated = true;
    }

    /// <summary>
    /// Log Format Builder for file output
    /// </summary>
    /// <param name="level"></param>
    /// <param name="input"></param>
    /// <param name="LogName"></param>
    public void LogFormatter(LogLevel level, string input, string LogName)
    {
        string logMessage = $"{DateTime.Now:HH:mm:ss} [{char.ToUpper(LogName[0]) + LogName[1..].ToLower()}/{level}]: {input}";
        WriteLogToFile(logMessage);
    }
    /// <summary>
    /// Write the log message to the file
    /// </summary>
    /// <param name="logMessage"></param>
    public void WriteLogToFile(string logMessage)
    {
        try
        {
            if (logPath != null && isLogFileCreated)
            {
                // Writing the log message to the file
                using StreamWriter writer = new(logPath, true);
                writer.WriteLine(logMessage);
            }
        }
        catch (Exception e)
        { _ = e; }
    }

    public void Cleanup()
    {
        // create the logs folder for archival purposes if it doesn't exist
        string oldLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(oldLogs))
        { Directory.CreateDirectory(oldLogs); }

        short dupe = 0;
        bool isCreating = true;

        while (isCreating)
        {
            try
            {
                if (dupe == 0)
                {
                    string fileName = $"{DateTime.Now:MM-dd-yyyy}.log";
                    string modifiedOldLogs = Path.Combine(oldLogs, fileName);
                    File.Copy(logPath, modifiedOldLogs);
                    isCreating = false;
                }
                else
                {
                    string fileNameDuped = $"{DateTime.Now:MM-dd-yyyy}_{dupe}.log";
                    string modifiedOldLogs1 = Path.Combine(oldLogs, fileNameDuped);
                    File.Copy(logPath, modifiedOldLogs1);
                    isCreating = false;
                }
            }
            catch (Exception e)
            { _ = e; }
            dupe++;
        }
    }
}