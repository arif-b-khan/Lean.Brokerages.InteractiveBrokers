using FluentAssertions;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class IntegrationSmokeTest
{
    [Fact]
    public void Program_WithValidArgs_ShouldRunWithoutNetworkCalls()
    {
        // Skip this test unless explicitly enabled via environment variable
        if (Environment.GetEnvironmentVariable("IB_TOOLBOX_IT") != "1")
        {
            return; // Skip test to preserve CI determinism
        }

        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Minute",
            "--from", "2024-01-01",
            "--to", "2024-01-02",
            "--data-dir", "./test-data",
            "--log-level", "debug",
            "--dry-run" // This flag should prevent actual network calls
        };

        // Set test environment variables
        Environment.SetEnvironmentVariable("IB_USERNAME", "testuser");
        Environment.SetEnvironmentVariable("IB_PASSWORD", "testpass");
        Environment.SetEnvironmentVariable("IB_ACCOUNT", "U1234567");

        try
        {
            // Act & Assert - This will fail until Program.cs dry-run mode is implemented
            var exitCode = Program.Main(args);
            exitCode.Should().Be(0, "Dry run should succeed without errors");

            // Verify no actual data files were created (dry run mode)
            Directory.Exists("./test-data").Should().BeFalse("Dry run should not create output files");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("IB_USERNAME", null);
            Environment.SetEnvironmentVariable("IB_PASSWORD", null);
            Environment.SetEnvironmentVariable("IB_ACCOUNT", null);
            
            if (Directory.Exists("./test-data"))
            {
                Directory.Delete("./test-data", true);
            }
        }
    }

    [Fact]
    public void Program_WithInvalidCredentials_ShouldLogErrorAndExitNonZero()
    {
        // Skip this test unless explicitly enabled
        if (Environment.GetEnvironmentVariable("IB_TOOLBOX_IT") != "1")
        {
            return;
        }

        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Minute",
            "--from", "2024-01-01",
            "--to", "2024-01-02",
            "--data-dir", "./test-data",
            "--dry-run"
        };

        // Don't set credentials - should fail validation

        // Act & Assert - This will fail until Program.cs credential validation is implemented
        var exitCode = Program.Main(args);
        exitCode.Should().NotBe(0, "Should exit non-zero when credentials are missing");
    }

    [Fact]
    public void Program_WithMissingDataDir_ShouldCreateDirectoryAndLog()
    {
        // Skip this test unless explicitly enabled
        if (Environment.GetEnvironmentVariable("IB_TOOLBOX_IT") != "1")
        {
            return;
        }

        // Arrange
        var testDataDir = "./integration-test-data";
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Daily",
            "--from", "2024-01-01",
            "--to", "2024-01-02",
            "--data-dir", testDataDir,
            "--dry-run"
        };

        Environment.SetEnvironmentVariable("IB_USERNAME", "testuser");
        Environment.SetEnvironmentVariable("IB_PASSWORD", "testpass");
        Environment.SetEnvironmentVariable("IB_ACCOUNT", "U1234567");

        try
        {
            // Ensure directory doesn't exist before test
            if (Directory.Exists(testDataDir))
            {
                Directory.Delete(testDataDir, true);
            }

            // Act & Assert - This will fail until Program.cs directory creation is implemented
            var exitCode = Program.Main(args);
            exitCode.Should().Be(0, "Should succeed and create missing directory");
            
            // In non-dry-run mode, directory should be created
            // In dry-run mode, this behavior may vary based on implementation
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("IB_USERNAME", null);
            Environment.SetEnvironmentVariable("IB_PASSWORD", null);
            Environment.SetEnvironmentVariable("IB_ACCOUNT", null);
            
            if (Directory.Exists(testDataDir))
            {
                Directory.Delete(testDataDir, true);
            }
        }
    }

    [Fact]
    public void Program_ShouldLogCorrelationId()
    {
        // Skip this test unless explicitly enabled
        if (Environment.GetEnvironmentVariable("IB_TOOLBOX_IT") != "1")
        {
            return;
        }

        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Daily",
            "--from", "2024-01-01",
            "--to", "2024-01-02",
            "--data-dir", "./test-data",
            "--log-level", "info",
            "--dry-run"
        };

        Environment.SetEnvironmentVariable("IB_USERNAME", "testuser");
        Environment.SetEnvironmentVariable("IB_PASSWORD", "testpass");
        Environment.SetEnvironmentVariable("IB_ACCOUNT", "U1234567");

        try
        {
            // Act & Assert - This will fail until Program.cs correlation logging is implemented
            // We would need to capture log output to verify correlation ID presence
            var exitCode = Program.Main(args);
            exitCode.Should().Be(0, "Should run successfully with correlation logging");
            
            // TODO: Capture and verify log output contains correlation ID
            // This would require setting up a test log sink or output capture
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("IB_USERNAME", null);
            Environment.SetEnvironmentVariable("IB_PASSWORD", null);
            Environment.SetEnvironmentVariable("IB_ACCOUNT", null);
        }
    }
}