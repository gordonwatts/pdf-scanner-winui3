using Microsoft.UI.Xaml;

namespace IndicoPenSpike;

public partial class App : Application
{
    private Window? _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = args;

        _window = new MainWindow();
        _window.Activate();
    }
}
