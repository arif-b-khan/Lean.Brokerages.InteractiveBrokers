# Quickstart: Run the Toolbox GUI (Dev)

Prerequisites:


Run (from repo root):

```bash
dotnet run --project ToolBox/QuantConnect.InteractiveBrokers.ToolBox -- gui
```

This will launch the cross-platform Avalonia GUI in development mode. From the GUI:

1. Open Settings → Enter IB credentials and optional overrides.
2. Select Lean Data Directory.
3. Choose a symbol and click "Load" to view data in the grid.
4. To perform downloads, open the "Downloads" tab, select date range and symbol, then click "Start".


## Credential Storage

IB credentials are stored securely per user using AES encryption. You can clear or update credentials from the Connection tab. Credentials are never logged or exposed in plaintext.

## Troubleshooting

- If the GUI fails to launch, ensure you have .NET 9.0 and Avalonia dependencies installed.
- For credential issues, use the "Clear" button in the Connection tab and re-enter your credentials.
- For data directory issues, verify the path and permissions.

## Contracts for GUI

This document outlines the contracts for the GUI application.

### GUI Contract

- The GUI must allow users to input their IB credentials securely.
- The GUI must provide a way to select the Lean Data Directory.
- The GUI must display data in a grid format after loading a symbol.
- The GUI must enable users to download data for specified date ranges and symbols.

### Security Considerations

- All sensitive information must be encrypted and stored securely.
- Non-sensitive overrides should be exportable to a `.env` file.
