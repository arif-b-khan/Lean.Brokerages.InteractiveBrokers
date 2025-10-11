using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

public class DownloadViewModel
{
    private readonly IGuiApi _api;

    public string Symbol { get; set; } = "SPY";
    public string Resolution { get; set; } = "daily";
    public DateTime From { get; set; } = DateTime.UtcNow.Date.AddDays(-7);
    public DateTime To { get; set; } = DateTime.UtcNow.Date;
    public string DataDir { get; set; } = Path.GetTempPath();

    public ObservableCollection<string> Jobs { get; } = new();

    public ICommand StartCommand { get; }

    // Parameterless constructor for design-time support
    public DownloadViewModel() : this(null!)
    {
    }

    public DownloadViewModel(IGuiApi api)
    {
        _api = api;
        StartCommand = new RelayCommand(async _ => await StartJob());
    }

    private async Task StartJob()
    {
        if (_api == null) return; // Design-time safety
        
        var request = new DownloadRequest
        {
            Symbol = Symbol,
            Resolution = Resolution,
            From = From,
            To = To,
            DataDir = DataDir
        };

        var job = await _api.StartDownloadJobAsync(request);
        Jobs.Add($"{job.JobId}: {job.Symbol} [{job.Status}]");
    }
}
