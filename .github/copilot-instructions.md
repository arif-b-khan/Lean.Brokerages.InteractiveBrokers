# Lean.Brokerages.InteractiveBrokers Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-01-27

## Active Technologies
- C# / .NET 9.0 (aligns with existing csproj in repo) (001-create-a-data)
- System.CommandLine for CLI argument parsing and help generation
- xUnit + FluentAssertions for comprehensive testing with TDD approach
- ILogger interface for structured logging with correlation IDs and secret redaction
- Interactive Brokers Gateway/TWS integration with TCP connectivity testing
- IBAutomater framework for gateway automation (CI environment detection)

## Project Structure
```
src/
tests/
ToolBox/
  QuantConnect.InteractiveBrokers.ToolBox/
    Program.cs                     # Main CLI entrypoint
    InteractiveBrokersDownloader.cs # IB data download logic
    StructuredLogger.cs            # Enhanced logging system
    IBAutomaterHelper.cs           # Gateway automation
  QuantConnect.InteractiveBrokers.ToolBox.Tests/
    *.cs                          # Comprehensive test suite
```

## Commands
- `dotnet run --project ToolBox/QuantConnect.InteractiveBrokers.ToolBox` - Run IB ToolBox
- `dotnet test ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests` - Run ToolBox tests
- CLI Usage: `--symbol SPY --resolution Daily --from-date 2023-01-01 --to-date 2023-12-31`

## Code Style
- C# / .NET 9.0: Follow standard conventions with async/await patterns
- TDD approach: Tests first, then implementation
- Structured logging with correlation IDs and secret redaction
- LEAN-compatible data directory structure and CSV formatting
- Deterministic test behavior with fixed seeds and no external calls

## Recent Changes
- 001-create-a-data: Added comprehensive IB ToolBox with CLI, structured logging, testing framework
- Implemented System.CommandLine for robust argument parsing
- Added IBAutomater integration framework with CI environment detection
- Created comprehensive test suite with performance benchmarks
- Added TCP connectivity testing with actionable error messages

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->