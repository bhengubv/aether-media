using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aether.Media.Core;
using Aether.Media.DependencyInjection;
using Aether.Media.Desktop.Services;
using Aether.Media.Desktop.ViewModels;
using Aether.Media.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Media.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow(vm);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddAetherMedia(media => media
            .AddIdentity()
            .AddContent()
            .AddSocial()
            .AddStreaming()
            .AddAI());

        // Desktop-local implementations for interfaces without a mesh-backed impl
        services.AddSingleton<LocalMediaFeed>();
        services.AddSingleton<IMediaFeed>(sp => sp.GetRequiredService<LocalMediaFeed>());
        services.AddSingleton<IMediaPlayer, LocalMediaPlayer>();
        services.AddSingleton<ICreatorChannel, LocalCreatorChannel>();

        // Register the root view model as transient so it is constructed fresh
        // each time (useful for test resets), but in practice the app only
        // requests it once.
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}