using System.Text;

namespace Untolia.Core.Diagnostics;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error
}

public sealed class Logger
{
    private readonly bool _echoToConsole;
    private readonly string _logFilePath;
    private readonly object _sync = new();

    public Logger(string logFilePath, bool echoToConsole = true)
    {
        _logFilePath = logFilePath;
        _echoToConsole = echoToConsole;

        try
        {
            var dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch
        {
            /* ignore */
        }
    }

    public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    public void Trace(string msg)
    {
        Write(LogLevel.Trace, msg);
    }

    public void Debug(string msg)
    {
        Write(LogLevel.Debug, msg);
    }

    public void Info(string msg)
    {
        Write(LogLevel.Info, msg);
    }

    public void Warn(string msg)
    {
        Write(LogLevel.Warn, msg);
    }

    public void Error(string msg)
    {
        Write(LogLevel.Error, msg);
    }

    public void Error(Exception ex, string context = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context)) sb.Append('[').Append(context).Append("] ");
        sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message).AppendLine();
        sb.Append(ex.StackTrace);
        Write(LogLevel.Error, sb.ToString());
    }

    private void Write(LogLevel level, string message)
    {
        if (level < MinimumLevel) return;

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_sync)
        {
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch
            {
                /* ignore */
            }

            if (_echoToConsole)
                try
                {
                    Console.WriteLine(line);
                }
                catch
                {
                    /* ignore */
                }
        }
    }
}