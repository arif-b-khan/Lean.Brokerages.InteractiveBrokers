using System.Threading.Tasks;
using System.Windows.Input;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

using QuantConnect.InteractiveBrokers.ToolBox.Security;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

public class ConnectionViewModel : INotifyPropertyChanged
{
    private readonly ICredentialStore _store;
    public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
    public string Account { get => _account; set { _account = value; OnPropertyChanged(); } }

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _account = string.Empty;

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConnectionViewModel() : this(new CredentialStore()) {}
    public ConnectionViewModel(ICredentialStore? store = null)
    {
        _store = store ?? new CredentialStore();
        SaveCommand = new RelayCommand(async _ => await Save());
        ClearCommand = new RelayCommand(async _ => await Clear());
        _ = Load();
    }

    private async Task Save()
    {
        await _store.SaveAsync("IB_USERNAME", Username, CancellationToken.None);
        await _store.SaveAsync("IB_PASSWORD", Password, CancellationToken.None);
        await _store.SaveAsync("IB_ACCOUNT", Account, CancellationToken.None);
    }

    private async Task Clear()
    {
        Username = string.Empty;
        Password = string.Empty;
        Account = string.Empty;
        await _store.DeleteAsync("IB_USERNAME", CancellationToken.None);
        await _store.DeleteAsync("IB_PASSWORD", CancellationToken.None);
        await _store.DeleteAsync("IB_ACCOUNT", CancellationToken.None);
    }

    private async Task Load()
    {
        Username = await _store.LoadAsync("IB_USERNAME") ?? string.Empty;
        Password = await _store.LoadAsync("IB_PASSWORD") ?? string.Empty;
        Account = await _store.LoadAsync("IB_ACCOUNT") ?? string.Empty;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
