using Avalonia;
using System;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        ToolBoxApp.BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
    }
}
