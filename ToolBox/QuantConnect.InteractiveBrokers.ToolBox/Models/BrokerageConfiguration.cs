using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuantConnect.InteractiveBrokers.ToolBox.Models;

/// <summary>
/// Represents persisted Interactive Brokers configuration values that power the Toolbox CLI and GUI.
/// </summary>
public sealed class BrokerageConfiguration
{
    public BrokerageConfiguration()
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    [JsonConstructor]
    public BrokerageConfiguration(
        Guid id,
        string username,
        string password,
        string account,
        string gatewayHost,
        int gatewayPort,
        string gatewayDirectory,
        string gatewayVersion,
        string tradingMode,
        bool automaterExportLogs,
        string dataDirectory,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Username = username;
        Password = password;
        Account = account;
        GatewayHost = gatewayHost;
        GatewayPort = gatewayPort;
        GatewayDirectory = gatewayDirectory;
        GatewayVersion = gatewayVersion;
        TradingMode = tradingMode;
        AutomaterExportLogs = automaterExportLogs;
        DataDirectory = dataDirectory;
        CreatedAtUtc = createdAtUtc == default ? DateTime.UtcNow : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? CreatedAtUtc : updatedAtUtc;
    }

    public Guid Id { get; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public string GatewayHost { get; set; } = "127.0.0.1";

    public int GatewayPort { get; set; } = 7497;

    public string GatewayDirectory { get; set; } = string.Empty;

    public string GatewayVersion { get; set; } = "latest";

    public string TradingMode { get; set; } = "paper";

    public bool AutomaterExportLogs { get; set; }

    public string DataDirectory { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Username))
        {
            errors.Add("Username (IB_USERNAME) is required.");
        }

        if (string.IsNullOrWhiteSpace(Account))
        {
            errors.Add("Account (IB_ACCOUNT) is required.");
        }

        if (string.IsNullOrWhiteSpace(DataDirectory))
        {
            errors.Add("DataDirectory (DATA_DIR) is required.");
        }

        if (GatewayPort is < 1 or > 65535)
        {
            errors.Add("GatewayPort must be between 1 and 65535.");
        }

        return errors;
    }

    public void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Dictionary<string, string> ToEnvironmentVariables()
    {
        var map = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = Username,
            ["IB_PASSWORD"] = Password,
            ["IB_ACCOUNT"] = Account,
            ["GATEWAY_HOST"] = GatewayHost,
            ["GATEWAY_PORT"] = GatewayPort.ToString(),
            ["IB_GATEWAY_DIR"] = GatewayDirectory,
            ["IB_VERSION"] = GatewayVersion,
            ["IB_TRADING_MODE"] = TradingMode,
            ["IB_AUTOMATER_EXPORT_LOGS"] = AutomaterExportLogs ? "true" : "false",
            ["DATA_DIR"] = DataDirectory
        };

        return map;
    }

    public static BrokerageConfiguration FromEnvironment(IDictionary<string, string> environment)
    {
        var configuration = new BrokerageConfiguration
        {
            Username = environment.TryGetValue("IB_USERNAME", out var username) ? username : string.Empty,
            Password = environment.TryGetValue("IB_PASSWORD", out var password) ? password : string.Empty,
            Account = environment.TryGetValue("IB_ACCOUNT", out var account) ? account : string.Empty,
            GatewayHost = environment.TryGetValue("GATEWAY_HOST", out var host) && !string.IsNullOrWhiteSpace(host) ? host : "127.0.0.1",
            GatewayPort = environment.TryGetValue("GATEWAY_PORT", out var port) && int.TryParse(port, out var value) ? value : 7497,
            GatewayDirectory = environment.TryGetValue("IB_GATEWAY_DIR", out var directory) ? directory : string.Empty,
            GatewayVersion = environment.TryGetValue("IB_VERSION", out var version) && !string.IsNullOrWhiteSpace(version) ? version : "latest",
            TradingMode = environment.TryGetValue("IB_TRADING_MODE", out var mode) && !string.IsNullOrWhiteSpace(mode) ? mode : "paper",
            AutomaterExportLogs = environment.TryGetValue("IB_AUTOMATER_EXPORT_LOGS", out var exportLogs) && bool.TryParse(exportLogs, out var parsedExport) && parsedExport,
            DataDirectory = environment.TryGetValue("DATA_DIR", out var dataDir) ? dataDir : string.Empty
        };

        configuration.Touch();
        return configuration;
    }
}
