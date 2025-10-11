using System;
using QuantConnect.InteractiveBrokers.ToolBox;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.Logging;

public class UiLoggerAdapter : ILogger
{
    private readonly Action<string, string> _logAction;
    
    public UiLoggerAdapter(Action<string, string> logAction)
    {
        _logAction = logAction;
    }
    
    public void LogTrace(string message, object? context = null) => _logAction("Trace", Redact(message));
    public void LogDebug(string message, object? context = null) => _logAction("Debug", Redact(message));
    public void LogInfo(string message, object? context = null) => _logAction("Info", Redact(message));
    public void LogWarning(string message, object? context = null) => _logAction("Warning", Redact(message));
    public void LogError(string message, object? context = null, Exception? exception = null) => 
        _logAction("Error", Redact($"{message}{(exception != null ? $" Exception: {exception}" : "")}"));
    
    private static string Redact(string msg)
    {
        // Simple secret redaction: replace IB_PASSWORD and similar keys
        return msg.Replace("IB_PASSWORD", "***").Replace("PASSWORD", "***");
    }
}
