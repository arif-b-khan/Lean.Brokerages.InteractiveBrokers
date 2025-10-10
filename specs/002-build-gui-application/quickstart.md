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

Notes:

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
