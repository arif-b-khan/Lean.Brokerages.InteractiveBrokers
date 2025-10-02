# QuantConnect Interactive Brokers ToolBox

A console application for downloading historical market data directly from Interactive Brokers and writing it in LEAN-compatible data directory structure.

## Overview

This ToolBox-style console app downloads historical data from Interactive Brokers using the existing brokerage integration and outputs data in LEAN format for backtesting and analysis.

## Prerequisites

- IBKR account with Market Data subscriptions
- IB Gateway or TWS available
- .NET 9.0 SDK

## Quick Start

See the detailed [Quickstart Guide](../../specs/001-create-a-data/quickstart.md) for complete setup and usage instructions.

### Basic Usage

```bash
# Download AAPL minute bars for January 2024
dotnet run -- AAPL equity minute "2024-01-01" "2024-01-31" ./data

# Download SPY daily data with custom gateway settings
dotnet run -- SPY equity daily "2023-01-01" "2023-12-31" ./data --gateway-host 192.168.1.100 --gateway-port 7497

# Validate configuration without downloading
dotnet run -- AAPL equity minute "2024-01-01" "2024-01-31" ./data --dry-run
```

### Command Line Options

```text
USAGE:
    QuantConnect.InteractiveBrokers.ToolBox <symbol> <security-type> <resolution> <from> <to> <data-dir> [OPTIONS]

ARGUMENTS:
    <symbol>          Stock symbol (e.g., AAPL, MSFT)
    <security-type>   Security type (equity, forex, future, option)
    <resolution>      Data resolution (tick, second, minute, hour, daily)
    <from>            Start date (YYYY-MM-DD)
    <to>              End date (YYYY-MM-DD) 
    <data-dir>        Output directory for data files

OPTIONS:
    --exchange, -e <exchange>          Exchange (default: SMART)
    --currency, -c <currency>          Currency (default: USD)  
    --config <config>                  JSON config file path (optional)
    --log-level <log-level>           Log level (trace, debug, info, warn, error)
    --gateway-host <gateway-host>     IB Gateway host (default: 127.0.0.1)
    --gateway-port <gateway-port>     IB Gateway port (default: 7497)
    --use-ib-automater               Use IBAutomater to manage IB Gateway automatically
    --dry-run                         Validate configuration without connecting
    --help                            Show help and usage information
```

## Configuration

Credentials can be provided via environment variables or JSON config file:

**Environment Variables:**

```bash
export IB_USERNAME="your-username"
export IB_PASSWORD="your-password"
export IB_ACCOUNT="U1234567"
export GATEWAY_HOST="127.0.0.1"    # Optional
export GATEWAY_PORT="7497"          # Optional
```

**JSON Configuration File:**

```json
{
  "IB_USERNAME": "your_username",
  "IB_PASSWORD": "your_password",
  "IB_ACCOUNT": "your_account_id",
  "GATEWAY_HOST": "127.0.0.1",
    "GATEWAY_PORT": "7497",
    "IB_GATEWAY_DIR": "/Users/your-user/Jts",
    "IB_VERSION": "latest",
    "IB_TRADING_MODE": "paper",
    "IB_AUTOMATER_EXPORT_LOGS": "false"
}
```

### Using IBAutomater

The `--use-ib-automater` flag lets the ToolBox launch and manage IB Gateway automatically. To enable it:

- Install IB Gateway locally and set `IB_GATEWAY_DIR` to its installation folder (`C:\\Jts` on Windows, `~/Jts` on macOS/Linux by default).
- Optionally set `IB_VERSION` (defaults to `latest`) and `IB_TRADING_MODE` (`paper` or `live`).
- Keep `GATEWAY_HOST` pointing to `127.0.0.1`—IBAutomater only manages local sessions.
- Run the ToolBox with `--use-ib-automater`; approve the 2FA prompt when requested.

If an existing IB Gateway session is already running, IBAutomater will skip starting a new one and reuse the active connection.

## Output Format

Data is saved in LEAN-compatible format:

- **Daily data**: `{data-dir}/equity/{symbol}/daily/{symbol}.csv`
- **Minute data**: `{data-dir}/equity/{symbol}/minute/{symbol}/{yyyyMMdd}_trade.csv`

CSV format: `DateTime,Open,High,Low,Close,Volume`

## Features

- **Cross-platform** (Windows, macOS, Linux)
- **LEAN-compatible** data format output
- **Rate limiting** with exponential backoff and jitter
- **Structured logging** with credential redaction and correlation IDs
- **Market session handling** with trading day filtering
- **IBAutomater integration** for automatic gateway management (optional)
- **Comprehensive validation** for dates, symbols, and configuration
- **Performance optimized** with atomic file writes and throughput monitoring
- **Support for multiple resolutions** (tick, second, minute, hour, daily)
- **Flexible configuration** via environment variables or JSON files

## Troubleshooting

**Connection Issues:**

- Ensure IB Gateway/TWS is running with API enabled
- Check gateway host/port settings
- Use `--dry-run` to validate configuration

**Rate Limiting:**

- Tool automatically handles IB rate limits with exponential backoff
- Monitor logs for paging/backoff events
- Consider smaller date ranges for large downloads

For detailed troubleshooting and advanced usage, see the [Quickstart Guide](../../specs/001-create-a-data/quickstart.md).

## Development

For detailed implementation information, see the [specification](../../specs/001-create-a-data/spec.md) and [implementation plan](../../specs/001-create-a-data/plan.md).

## Architecture

The application follows the Constitution principles:

- Tests-first development
- Deterministic CI (no external calls in tests)
- Behavior parity with LEAN and IB APIs
- Observability with safe logging
- Thread-safe and cancellable operations

For detailed implementation information, see the [specification](../../specs/001-create-a-data/spec.md) and [implementation plan](../../specs/001-create-a-data/plan.md).
