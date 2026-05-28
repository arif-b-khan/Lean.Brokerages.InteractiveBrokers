using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

public class SnapshotViewModel
{
    private readonly IGuiApi _api;

    public string Symbol { get; set; } = "SPY";
    public string Resolution { get; set; } = "daily";
    public string SecurityType { get; set; } = "equity";
    public string DataDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    // Avalonia DatePicker.SelectedDate uses DateTimeOffset?; provide wrappers for binding
    public DateTimeOffset? StartDateOffset
    {
        get => new DateTimeOffset(StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        set
        {
            if (value.HasValue)
            {
                StartDate = DateOnly.FromDateTime(value.Value.UtcDateTime);
            }
        }
    }

    public DateTimeOffset? EndDateOffset
    {
        get => new DateTimeOffset(EndDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        set
        {
            if (value.HasValue)
            {
                EndDate = DateOnly.FromDateTime(value.Value.UtcDateTime);
            }
        }
    }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;

    public ObservableCollection<BarRecord> Records { get; } = new();
    public ObservableCollection<string> SourceFiles { get; } = new();

    public int TotalRecords { get; private set; }
    public int TotalPages { get; private set; }

    public ICommand LoadCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }

    // Parameterless constructor for design-time support
    public SnapshotViewModel() : this(null!)
    {
    }

    public SnapshotViewModel(IGuiApi api)
    {
        _api = api;
        LoadCommand = new RelayCommand(async _ => await LoadPage(1));
        NextPageCommand = new RelayCommand(async _ => await LoadPage(PageNumber + 1));
        PrevPageCommand = new RelayCommand(async _ => await LoadPage(Math.Max(1, PageNumber - 1)));
    }

    private async Task LoadPage(int page)
    {
        var request = new SnapshotRequest
        {
            Symbol = Symbol,
            Resolution = Resolution,
            SecurityType = SecurityType,
            DataDirectory = DataDirectory,
            StartDate = StartDate,
            EndDate = EndDate,
            PageNumber = page,
            PageSize = PageSize
        };

        if (_api == null) return; // Design-time safety
        
        var pageResult = await _api.LoadSnapshotAsync(request);

        Records.Clear();
        foreach (var r in pageResult.Snapshot.Records)
        {
            Records.Add(r);
        }

        SourceFiles.Clear();
        foreach (var f in pageResult.Snapshot.SourceFiles)
        {
            SourceFiles.Add(f);
        }

        PageNumber = pageResult.PageNumber;
        PageSize = pageResult.PageSize;
        TotalRecords = pageResult.TotalRecords;
        TotalPages = pageResult.TotalPages;
    }
}
