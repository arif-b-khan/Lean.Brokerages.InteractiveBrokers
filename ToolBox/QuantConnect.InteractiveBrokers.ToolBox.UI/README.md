# QuantConnect.InteractiveBrokers.ToolBox.UI

Minimal UI runner for the Interactive Brokers ToolBox. This project hosts an Avalonia-based GUI that reuses the CLI's data-fetching logic.

Quick commands

Build the UI project:

```bash
dotnet build ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/QuantConnect.InteractiveBrokers.ToolBox.UI.csproj -c Debug
```

Run the UI locally:

```bash
dotnet run --project ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/QuantConnect.InteractiveBrokers.ToolBox.UI.csproj -c Debug
```

Credential Storage
------------------
IB credentials are stored securely per user using AES encryption. You can clear or update credentials from the Connection tab. Credentials are never logged or exposed in plaintext.

DataGrid Usage
--------------
The snapshot viewer uses Avalonia's DataGrid with virtualization for fast paging of large datasets. If you experience performance issues, ensure your system meets Avalonia's requirements.

Troubleshooting
---------------
- If the GUI fails to launch, ensure you have .NET 9.0 and Avalonia dependencies installed.
- For credential issues, use the "Clear" button in the Connection tab and re-enter your credentials.
- For data directory issues, verify the path and permissions.

