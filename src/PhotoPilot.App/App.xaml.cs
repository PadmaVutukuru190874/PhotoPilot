using System.Windows;
using PhotoPilot.App.Views;

namespace PhotoPilot.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow window = new();
        window.Show();
    }
}