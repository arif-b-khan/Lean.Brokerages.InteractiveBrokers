using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Gui;
using QuantConnect.InteractiveBrokers.ToolBox;
using QuantConnect.InteractiveBrokers.ToolBox.Services;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI;

public partial class ToolBoxApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
    // Configure DI
    var services = new ServiceCollection();

        // Register shared services from the CLI project
    services.AddSingleton<OutputLayout>();
    services.AddSingleton<BackoffPolicy>();
    services.AddSingleton<QuantConnect.InteractiveBrokers.ToolBox.Services.DownloadJobManager>();
        services.AddSingleton<ILogger>(provider => new StructuredLogger("info", Guid.NewGuid().ToString("N")[..8]));

        // Register downloader and writer from CLI
        // Register IDataDownloader implementation
        services.AddTransient<IDataDownloader>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var backoff = provider.GetRequiredService<BackoffPolicy>();
            return new InteractiveBrokersDownloader(new Dictionary<string,string>(), backoff, logger);
        });

        services.AddTransient<DataWriter>(provider =>
        {
            var output = provider.GetRequiredService<OutputLayout>();
            var logger = provider.GetRequiredService<ILogger>();
            return new DataWriter(output, logger);
        });

        services.AddSingleton<ILeanDataSnapshotLoader>(provider =>
        {
            var output = provider.GetRequiredService<OutputLayout>();
            var logger = provider.GetRequiredService<ILogger>();
            return new LeanDataSnapshotLoader(output, logger);
        });

        // Register GuiService as IGuiApi
        services.AddSingleton<QuantConnect.InteractiveBrokers.ToolBox.UI.Api.IGuiApi>(provider =>
        {
            var jm = provider.GetRequiredService<QuantConnect.InteractiveBrokers.ToolBox.Services.DownloadJobManager>();
            var downloader = provider.GetRequiredService<IDataDownloader>();
            var writer = provider.GetRequiredService<DataWriter>();
            var snapshotLoader = provider.GetRequiredService<ILeanDataSnapshotLoader>();
            var logger = provider.GetRequiredService<ILogger>();
            return new QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService(jm, downloader, writer, snapshotLoader, logger);
        });

        // Register MainViewModel and GuiMainWindow so they can be resolved via DI
        services.AddSingleton<QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels.MainViewModel>(provider =>
        {
            var guiApi = provider.GetRequiredService<QuantConnect.InteractiveBrokers.ToolBox.UI.Api.IGuiApi>();
            return new QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels.MainViewModel(guiApi);
        });

        services.AddSingleton<GuiMainWindow>(provider =>
        {
            var window = new GuiMainWindow();
            // inject DataContext if needed
            var vm = provider.GetRequiredService<QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels.MainViewModel>();
            window.DataContext = vm;
            return window;
        });

        var serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve main window via DI if available
            var main = serviceProvider.GetService<GuiMainWindow>();
            if (main is null)
            {
                desktop.MainWindow = new GuiMainWindow();
            }
            else
            {
                desktop.MainWindow = main;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ToolBoxApp>()
                  .UsePlatformDetect()
                  .LogToTrace();
}
