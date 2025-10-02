using System.Diagnostics;
using Xunit;
using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

/// <summary>
/// Performance microbenchmarks and timing tests for serialization throughput
/// </summary>
public class PerformanceBenchmarkTests
{
    [Fact]
    public void Serialization_Throughput_ShouldMeetMinimumRequirements()
    {
        // Arrange
        var outputLayout = new OutputLayout();
        var bars = GenerateTestBars(10000); // 10K bars for benchmarking
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        
        var serializedBars = new List<string>();
        foreach (var bar in bars)
        {
            var csvLine = outputLayout.SerializeBar("minute", bar);
            serializedBars.Add(csvLine);
        }
        
        stopwatch.Stop();

        // Assert
        var barsPerSecond = bars.Count / stopwatch.Elapsed.TotalSeconds;
        var throughputMessage = $"Serialization throughput: {barsPerSecond:F0} bars/second ({stopwatch.ElapsedMilliseconds}ms for {bars.Count} bars)";
        
        // Log the performance result
        Console.WriteLine(throughputMessage);
        
        // Minimum requirement: should serialize at least 1000 bars per second
        barsPerSecond.Should().BeGreaterThan(1000, 
            "serialization should be fast enough for practical use");
        
        // All bars should be properly serialized
        serializedBars.Should().HaveCount(bars.Count);
        serializedBars.Should().AllSatisfy(line => 
            line.Should().NotBeNullOrEmpty("each bar should serialize to valid CSV"));
    }

    [Theory]
    [InlineData(1000)]    // 1K bars
    [InlineData(10000)]   // 10K bars
    [InlineData(100000)]  // 100K bars
    public void DataWriter_Throughput_ShouldScaleLinearly(int barCount)
    {
        // Arrange
        var logger = new ConsoleLogger("error", "perf-test"); // Only log errors to avoid output noise
        var outputLayout = new OutputLayout();
        var dataWriter = new DataWriter(outputLayout, logger);
        var bars = GenerateTestBars(barCount);
        
        var request = new DownloadRequest
        {
            Symbol = "TEST",
            Resolution = "minute",
            From = DateTime.Today.AddDays(-1),
            To = DateTime.Today,
            DataDir = Path.GetTempPath()
        };

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = dataWriter.WriteBars(request, bars).Result;
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue("write operation should succeed");
        
        var barsPerSecond = barCount / stopwatch.Elapsed.TotalSeconds;
        var throughputMessage = $"Write throughput ({barCount} bars): {barsPerSecond:F0} bars/second ({stopwatch.ElapsedMilliseconds}ms)";
        
        Console.WriteLine(throughputMessage);
        
        // Performance requirements based on bar count
        var minimumThroughput = barCount switch
        {
            <= 1000 => 500,    // 500 bars/sec for small datasets
            <= 10000 => 1000,  // 1K bars/sec for medium datasets  
            _ => 2000           // 2K bars/sec for large datasets
        };
        
        barsPerSecond.Should().BeGreaterThan(minimumThroughput,
            $"write throughput should meet minimum requirement for {barCount} bars");

        // Clean up test files
        foreach (var file in result.Files)
        {
            var fullPath = Path.Combine(request.DataDir, file);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    [Fact]
    public void ConfigLoader_Performance_ShouldBeReasonable()
    {
        // Arrange
        var logger = new ConsoleLogger("info", "perf-test");
        var configLoader = new ConfigLoader(logger);
        var stopwatch = new Stopwatch();
        
        // Set minimal environment variables for test
        Environment.SetEnvironmentVariable("IB_USERNAME", "test_user");
        Environment.SetEnvironmentVariable("IB_PASSWORD", "test_pass");
        Environment.SetEnvironmentVariable("IB_ACCOUNT", "test_account");

        try
        {
            // Act
            stopwatch.Start();
            var config = configLoader.LoadConfig(null).Result; // Load from environment
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "config loading should be fast");
            config.Should().NotBeNull("config should be loaded");
        }
        finally
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("IB_USERNAME", null);
            Environment.SetEnvironmentVariable("IB_PASSWORD", null);
            Environment.SetEnvironmentVariable("IB_ACCOUNT", null);
        }
    }

    [Theory]
    [InlineData("minute", 390)]   // 6.5 hours of minute bars
    [InlineData("daily", 252)]    // 1 year of daily bars
    public void Memory_Usage_ShouldBeReasonable(string resolution, int barCount)
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var bars = GenerateTestBars(barCount);
        
        // Act
        var outputLayout = new OutputLayout();
        var serializedBars = new List<string>();
        
        foreach (var bar in bars)
        {
            serializedBars.Add(outputLayout.SerializeBar(resolution, bar));
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;
        
        // Assert
        var memoryPerBar = memoryUsed / (double)barCount;
        var memoryMessage = $"Memory usage ({resolution}): {memoryUsed / 1024.0:F1} KB total, {memoryPerBar:F1} bytes per bar";
        
        Console.WriteLine(memoryMessage);
        
        // Memory usage should be reasonable (less than 1KB per bar)
        memoryPerBar.Should().BeLessThan(1024, "memory usage per bar should be reasonable");
    }

    /// <summary>
    /// Generate test bars for performance testing
    /// </summary>
    private static List<Bar> GenerateTestBars(int count)
    {
        var bars = new List<Bar>();
        var random = new Random(42); // Fixed seed for reproducible results
        var baseTime = new DateTime(2023, 6, 15, 9, 30, 0);
        
        for (int i = 0; i < count; i++)
        {
            var time = baseTime.AddMinutes(i);
            var open = 100.0m + (decimal)(random.NextDouble() * 10);
            var high = open + (decimal)(random.NextDouble() * 2);
            var low = open - (decimal)(random.NextDouble() * 2);
            var close = low + (decimal)(random.NextDouble() * (double)(high - low));
            var volume = 1000 + random.Next(10000);
            
            bars.Add(new Bar(time, open, high, low, close, volume));
        }
        
        return bars;
    }
}

