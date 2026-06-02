using Microsoft.UI.Xaml;

namespace IndicoPenSpike;

public partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = args;

        var window = new MainWindow();
        window.Activate();
    }
}
