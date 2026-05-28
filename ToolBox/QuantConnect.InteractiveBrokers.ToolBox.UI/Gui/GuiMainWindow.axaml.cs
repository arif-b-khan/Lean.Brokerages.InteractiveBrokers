using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui;

public partial class LegacyGuiMainWindow : Window
{
        public LegacyGuiMainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
