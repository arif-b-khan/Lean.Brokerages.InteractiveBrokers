using FluentAssertions;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class ConfigSchemaTests
{
    [Fact]
    public void LoadConfig_WithMissingRequiredKeys_ShouldThrow()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var incompleteConfig = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "testuser"
            // Missing IB_PASSWORD and IB_ACCOUNT
        };

        // Act & Assert - This will fail until ConfigLoader.cs is implemented
        var exception = Assert.Throws<ArgumentException>(() => 
            configLoader.ValidateConfig(incompleteConfig));
        exception.Message.Should().Contain("IB_PASSWORD", "Should indicate missing password");
        exception.Message.Should().Contain("IB_ACCOUNT", "Should indicate missing account");
    }

    [Fact]
    public void LoadConfig_WithAllRequiredKeys_ShouldSucceed()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var completeConfig = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "testuser",
            ["IB_PASSWORD"] = "testpass",
            ["IB_ACCOUNT"] = "U1234567"
        };

        // Act & Assert - This will fail until ConfigLoader.cs is implemented
        var result = configLoader.ValidateConfig(completeConfig);
        result.Should().BeTrue("Should pass validation with all required keys");
    }

    [Fact]
    public void LoadConfig_FromEnvironment_ShouldTakePrecedence()
    {
        // Arrange
        Environment.SetEnvironmentVariable("IB_USERNAME", "env_user");
        Environment.SetEnvironmentVariable("IB_PASSWORD", "env_pass");
        Environment.SetEnvironmentVariable("IB_ACCOUNT", "U7654321");

        var fileConfig = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "file_user",
            ["IB_PASSWORD"] = "file_pass",
            ["IB_ACCOUNT"] = "U1234567"
        };

        var configLoader = new ConfigLoader();

        try
        {
            // Act & Assert - This will fail until ConfigLoader.cs is implemented
            var config = configLoader.MergeConfigs(fileConfig);
            config["IB_USERNAME"].Should().Be("env_user", "Environment should override file config");
            config["IB_PASSWORD"].Should().Be("env_pass", "Environment should override file config");
            config["IB_ACCOUNT"].Should().Be("U7654321", "Environment should override file config");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("IB_USERNAME", null);
            Environment.SetEnvironmentVariable("IB_PASSWORD", null);
            Environment.SetEnvironmentVariable("IB_ACCOUNT", null);
        }
    }

    [Fact]
    public void LoadConfig_ShouldRedactSecretsInLogs()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var config = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "testuser",
            ["IB_PASSWORD"] = "secret123",
            ["IB_ACCOUNT"] = "U1234567"
        };

        // Act & Assert - This will fail until ConfigLoader.cs is implemented
        var logString = configLoader.GetRedactedConfigString(config);
        logString.Should().Contain("IB_USERNAME=testuser", "Username should be visible");
        logString.Should().Contain("IB_PASSWORD=***", "Password should be redacted");
        logString.Should().Contain("IB_ACCOUNT=U1234567", "Account should be visible");
        logString.Should().NotContain("secret123", "Secret should not appear in logs");
    }

    [Fact]
    public void LoadConfig_FromJsonFile_ShouldParseCorrectly()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var jsonContent = """
        {
            "IB_USERNAME": "jsonuser",
            "IB_PASSWORD": "jsonpass",
            "IB_ACCOUNT": "U9876543",
            "GATEWAY_HOST": "192.168.1.100",
            "GATEWAY_PORT": 7497,
            "LOG_LEVEL": "debug"
        }
        """;

        // Act & Assert - This will fail until ConfigLoader.cs is implemented
        var config = configLoader.ParseJsonConfig(jsonContent);
        config["IB_USERNAME"].Should().Be("jsonuser");
        config["IB_PASSWORD"].Should().Be("jsonpass");
        config["IB_ACCOUNT"].Should().Be("U9876543");
        config["GATEWAY_HOST"].Should().Be("192.168.1.100");
        config["GATEWAY_PORT"].Should().Be("7497");
        config["LOG_LEVEL"].Should().Be("debug");
    }

    [Fact]
    public void LoadConfig_WithInvalidJson_ShouldThrow()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var invalidJson = "{ invalid json }";

        // Act & Assert - This will fail until ConfigLoader.cs is implemented
        var exception = Assert.Throws<ArgumentException>(() => 
            configLoader.ParseJsonConfig(invalidJson));
        exception.Message.Should().Contain("JSON", "Should indicate JSON parsing error");
    }
}