using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI;

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
