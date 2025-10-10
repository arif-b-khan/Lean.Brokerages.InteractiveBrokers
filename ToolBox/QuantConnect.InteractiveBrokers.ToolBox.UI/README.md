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

Notes
- The UI project references the CLI project (`ToolBox/QuantConnect.InteractiveBrokers.ToolBox`) via a ProjectReference so it can reuse download/data-fetching logic.
- We intentionally avoid adding a ProjectReference from the CLI back to the UI to prevent circular project references. Build the solution normally; the UI project builds as part of the solution because it references the CLI.
- If your environment cannot render GUIs (CI/headless), run only the CLI project. The UI requires a graphical environment.

