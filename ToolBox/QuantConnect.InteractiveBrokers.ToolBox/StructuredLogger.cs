using System.Text.Json;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Common logging interface for structured logging
/// </summary>
public interface ILogger
{
    void LogTrace(string message, object? context = null);
    void LogDebug(string message, object? context = null);
    void LogInfo(string message, object? context = null);
    void LogWarning(string message, object? context = null);
    void LogError(string message, object? context = null, Exception? exception = null);
}

/// <summary>
/// Enhanced logger with structured logging, correlation IDs, and secret redaction
/// </summary>
public class StructuredLogger : ILogger
{
    private readonly string _logLevel;
    private readonly string _correlationId;
    private readonly LogLevel _currentLevel;

    public StructuredLogger(string logLevel, string correlationId)
    {
        _logLevel = logLevel;
        _correlationId = correlationId;
        _currentLevel = ParseLogLevel(logLevel);
    }

    public void LogTrace(string message, object? context = null)
    {
        if (_currentLevel <= LogLevel.Trace)
        {
            WriteLog("TRACE", message, context);
        }
    }

    public void LogDebug(string message, object? context = null)
    {
        if (_currentLevel <= LogLevel.Debug)
        {
            WriteLog("DEBUG", message, context);
        }
    }

    public void LogInfo(string message, object? context = null)
    {
        if (_currentLevel <= LogLevel.Info)
        {
            WriteLog("INFO", message, context);
        }
    }

    public void LogWarning(string message, object? context = null)
    {
        if (_currentLevel <= LogLevel.Warning)
        {
            WriteLog("WARN", message, context);
        }
    }

    public void LogError(string message, object? context = null, Exception? exception = null)
    {
        if (_currentLevel <= LogLevel.Error)
        {
            WriteLog("ERROR", message, context, exception);
        }
    }

    /// <summary>
    /// Log structured data with automatic secret redaction
    /// </summary>
    public void LogStructured(LogLevel level, string message, object data)
    {
        if (_currentLevel <= level)
        {
            var redactedData = RedactSecrets(data);
            WriteLog(level.ToString().ToUpperInvariant(), message, redactedData);
        }
    }

    private void WriteLog(string level, string message, object? context = null, Exception? exception = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = new
        {
            timestamp,
            level,
            correlationId = _correlationId,
            message,
            context = context is not null ? RedactSecrets(context) : null,
            exception = exception?.ToString()
        };

        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(logEntry, jsonOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// Redact sensitive information from logged objects
    /// </summary>
    private static object RedactSecrets(object obj)
    {
        if (obj == null) return null!;
        
        try
        {
            var json = JsonSerializer.Serialize(obj);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return RedactJsonElement(element);
        }
        catch
        {
            // If serialization fails, return string representation with basic redaction
            var str = obj.ToString() ?? "";
            return str.Contains("password") || str.Contains("secret") || str.Contains("key") 
                ? "[REDACTED]" 
                : str;
        }
    }    private static object RedactJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name.ToLowerInvariant();
                    var value = property.Value;
                    
                    // Redact sensitive keys
                    if (IsSensitiveKey(key))
                    {
                        dict[property.Name] = "[REDACTED]";
                    }
                    else
                    {
                        dict[property.Name] = RedactJsonElement(value);
                    }
                }
                return dict;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(RedactJsonElement).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return string.Empty;

            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Check if a key contains sensitive information that should be redacted
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        var sensitivePatterns = new[]
        {
            "password", "pwd", "pass", "secret", "key", "token", "auth",
            "credential", "login", "username", "user", "account", "api_key",
            "private", "sensitive", "security", "cert", "certificate"
        };

        return sensitivePatterns.Any(pattern => key.Contains(pattern));
    }

    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" or "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };
    }
}

/// <summary>
/// Log levels for structured logging
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4
}

/// <summary>
/// Extensions for backwards compatibility
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Create a structured logger from console logger settings
    /// </summary>
    public static StructuredLogger ToStructured(this ConsoleLogger consoleLogger)
    {
        return new StructuredLogger("info", Guid.NewGuid().ToString("N")[..8]);
    }
}