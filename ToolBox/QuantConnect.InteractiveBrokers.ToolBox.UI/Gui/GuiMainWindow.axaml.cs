using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui;

public partial class GuiMainWindow : Window
{
    public GuiMainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
