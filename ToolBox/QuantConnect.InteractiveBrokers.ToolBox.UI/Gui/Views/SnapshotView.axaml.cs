using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.Views;

public partial class SnapshotView : UserControl
{
    public SnapshotView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}