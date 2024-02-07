using System.Text.RegularExpressions;

namespace Utilities;

public sealed class Logger : Configure
{
    #region Properties
    private string LogName { get; set; }

    private readonly bool _verbose;
    private readonly bool _debug;
    private readonly string logPath = string.Empty;
    private readonly bool isLogFileCreated = false;

    #endregion
    #region Constructor

    public Logger(string logName)
    {
        LogName = logName;

        if (isLogFileCreated) return;
        // FIXME: monitor this in the future, just in case the properties are changed (since you moved the if-guard statement higher than this)
        if (Properties != null)
        {
            _verbose = Properties.IsVerbose;
            _debug = Properties.IsDebug;
        }

        // Delete the latest.log if it exist, otherwise just store the directory
        string logDirectory = AppDomain.CurrentDomain.BaseDirectory;
        logPath = Path.Combine(logDirectory, "latest.log");

        if (!Directory.Exists(logPath))
        {
            File.Delete(logPath);
        }

        isLogFileCreated = true;
    }

    public static void Cleanup()
    {
        // create the logs folder for archival purposes if it doesn't exist
        string oldLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(oldLogs))
        {
            Directory.CreateDirectory(oldLogs);
        }

        // update oldLogFolder to enable file renaming
        short dupe = 0;
        string modifiedOldLogs;
        string modifiedOldLogs1;

        bool isCreating = true;
        Exception? exception = null;


        while (isCreating)
        {
            // if File not found :: vscode debugging temporary fix :: "File.Copy() base directory points at the project location, not at the executable location"
            if (exception == null) {; }
            else
            {
                if (Regex.IsMatch(exception.Message, @"\bCould not find file\b")) break;
            }

            try
            {
                if (dupe == 0)
                {
                    string fileName = $"{DateTime.Now:MM-dd-yyyy}.log";
                    modifiedOldLogs = Path.Combine(oldLogs, fileName);
                    File.Copy("latest.log", modifiedOldLogs);
                    isCreating = false;
                }
                else
                {
                    string fileNameDuped = $"{DateTime.Now:MM-dd-yyyy}_{dupe}.log";
                    modifiedOldLogs1 = Path.Combine(oldLogs, fileNameDuped);
                    File.Copy("latest.log", modifiedOldLogs1);
                    isCreating = false;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            dupe++;
        }
    }

    #endregion
    #region Logging Methods

    private enum LogLevel
    {
        INFO,
        WARN,
        ERROR,
        FATAL,
        DEBUG,
        VERBOSE
    }
    public void Info(dynamic input, bool toWindow = false)
    {
        LogFormatter(LogLevel.INFO, input.ToString());
        if (toWindow) { Console.WriteLine(input); }
    }
    public void Warn(dynamic input)
    {
        LogFormatter(LogLevel.WARN, input.ToString());
    }
    public void Error(dynamic input, bool toWindow = false)
    {
        LogFormatter(LogLevel.ERROR, input.ToString());
        string exclusiveLogMessage = $"[{LogLevel.ERROR}] [{LogName.ToUpper()}] {input}";
        if (toWindow) { Console.WriteLine(exclusiveLogMessage); }
    }
    public void Fatal(dynamic input, bool toWindow = false)
    {
        LogFormatter(LogLevel.FATAL, input.ToString());
        string exclusiveLogMessage = $"[{LogLevel.FATAL}] [{LogName.ToUpper()}] {input}";
        if (toWindow) { Console.WriteLine(exclusiveLogMessage); }
    }
    public void Debug(dynamic input)
    {
        if (!_debug) return;
        LogFormatter(LogLevel.DEBUG, input.ToString());
    }
    public void Verbose(dynamic input)
    {
        if (!_verbose) return;
        LogFormatter(LogLevel.VERBOSE, input.ToString());
    }

    #endregion
    #region File Streaming

    private void LogFormatter(LogLevel level, string input)
    {
        string logMessage = $"{DateTime.Now:HH:mm:ss} [{char.ToUpper(LogName[0]) + LogName[1..].ToLower()}/{level}]: {input}";
        WriteLogToFile(logMessage);
    }
    private void WriteLogToFile(string logMessage)
    {
        try
        {
            if (logPath != null && isLogFileCreated)
            {
                // Writing the log message to the file

                using StreamWriter writer = new(logPath, true);
                // ->
                // byte[] contentBytes = Encoding.UTF8.GetBytes();
                writer.WriteLine(logMessage);
            }
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    #endregion

    private void HandleError(Exception e)
    {
        Logger Log = new("Log Handler");
        Log.Error(e, true);
    }
}