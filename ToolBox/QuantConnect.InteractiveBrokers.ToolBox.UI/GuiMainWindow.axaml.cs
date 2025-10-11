using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui;

public partial class GuiMainWindow : Window
{
    public GuiMainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            DataContext = new MainViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
