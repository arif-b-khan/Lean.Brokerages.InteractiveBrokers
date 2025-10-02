# Quickstart: IB Data Download ToolBox

This guide shows how to run the console app to download historical data from Interactive Brokers and write it in LEAN format.

## Prerequisites

- IBKR account with Market Data subscriptions
- IB Gateway or TWS available (IBAutomater can manage sessions)
- .NET SDK matching repo (net9.0)

## Configure Credentials

Preferred via environment variables:

```bash
export IB_USERNAME="your-username"
export IB_PASSWORD="your-password"
export IB_ACCOUNT="U1234567"
```

Or via a JSON config file and the --config flag:

```json
{
  "IB_USERNAME": "your-username",
  "IB_PASSWORD": "your-password",
  "IB_ACCOUNT": "U1234567"
}
```

## Run a Download

```bash
# Equity minute bars for January 2024
ib-toolbox-download \
  --symbol AAPL \
  --security-type Equity \
  --exchange SMART \
  --currency USD \
  --resolution Minute \
  --from 2024-01-01 \
  --to 2024-01-31 \
  --data-dir ./data
```

Expected output: LEAN-compatible files under ./data/equity/usa/minute/a/aapl/ with progress logs.

## Notes

- Respect IB pacing; long ranges may take time
- Use smaller date windows to avoid throttling
- Logs exclude secrets; use verbose mode for troubleshooting
