using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

public class MainViewModel
{
    private readonly IGuiApi _gui;

    public ICommand StartCommand { get; }

    public ObservableCollection<string> Jobs { get; } = new();

    public MainViewModel(IGuiApi gui)
    {
        _gui = gui;
        StartCommand = new RelayCommand(async _ => await StartJob());
    }

    private async Task StartJob()
    {
        var request = new DownloadRequest
        {
            Symbol = "TEST",
            Resolution = "daily",
            From = DateTime.UtcNow.Date.AddDays(-1),
            To = DateTime.UtcNow.Date,
            DataDir = Path.GetTempPath()
        };

        var job = await _gui.StartDownloadJobAsync(request);
        Jobs.Add($"{job.JobId}: {job.Symbol} [{job.Status}]");
    }
}
