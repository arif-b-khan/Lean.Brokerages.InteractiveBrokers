using System.Text.Json;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Configuration loader that handles environment variables and JSON files with secret redaction
/// </summary>
public class ConfigLoader
{
    private readonly ILogger? _logger;
    private readonly string[] _requiredKeys = ["IB_USERNAME", "IB_PASSWORD", "IB_ACCOUNT"];
    private readonly string[] _secretKeys = ["IB_PASSWORD"];

    public ConfigLoader(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load configuration from environment and optional JSON file
    /// </summary>
    public async Task<Dictionary<string, string>> LoadConfig(string? configPath = null)
    {
        var config = new Dictionary<string, string>();

        // Load from JSON file if provided
        if (!string.IsNullOrEmpty(configPath))
        {
            if (!File.Exists(configPath))
            {
                throw new ArgumentException($"Config file not found: {configPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(configPath);
            var fileConfig = ParseJsonConfig(jsonContent);
            
            foreach (var kvp in fileConfig)
            {
                config[kvp.Key] = kvp.Value;
            }
            
            _logger?.LogDebug($"Loaded configuration from file: {configPath}");
        }

        // Environment variables override file config
        config = MergeConfigs(config);

        // Validate required keys
        ValidateConfig(config);

        _logger?.LogInfo("Configuration loaded and validated successfully");
        _logger?.LogDebug(GetRedactedConfigString(config));

        return config;
    }

    /// <summary>
    /// Parse JSON configuration content
    /// </summary>
    public Dictionary<string, string> ParseJsonConfig(string jsonContent)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var config = new Dictionary<string, string>();

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? "",
                    JsonValueKind.Number => property.Value.GetInt32().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => property.Value.ToString()
                };
                
                config[property.Name] = value;
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Merge file config with environment variables (env takes precedence)
    /// </summary>
    public Dictionary<string, string> MergeConfigs(Dictionary<string, string> fileConfig)
    {
        var mergedConfig = new Dictionary<string, string>(fileConfig);

        // Environment variables override file config
        foreach (var key in _requiredKeys.Concat(new[] { "GATEWAY_HOST", "GATEWAY_PORT", "DATA_DIR", "LOG_LEVEL" }))
        {
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                mergedConfig[key] = envValue;
            }
        }

        return mergedConfig;
    }

    /// <summary>
    /// Validate that all required configuration keys are present
    /// </summary>
    public bool ValidateConfig(Dictionary<string, string> config)
    {
        var missingKeys = _requiredKeys.Where(key => !config.ContainsKey(key) || string.IsNullOrEmpty(config[key])).ToList();

        if (missingKeys.Any())
        {
            var missingKeysStr = string.Join(", ", missingKeys);
            throw new ArgumentException($"Missing required configuration keys: {missingKeysStr}");
        }

        return true;
    }

    /// <summary>
    /// Get configuration string with secrets redacted for safe logging
    /// </summary>
    public string GetRedactedConfigString(Dictionary<string, string> config)
    {
        var redactedPairs = config.Select(kvp => 
        {
            var value = _secretKeys.Contains(kvp.Key) ? "***" : kvp.Value;
            return $"{kvp.Key}={value}";
        });

        return string.Join(", ", redactedPairs);
    }
}