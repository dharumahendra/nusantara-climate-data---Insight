using System.Windows;

namespace NADI.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show("Error: " + args.Exception.ToString(), "Unhandled Exception");
            args.Handled = true;
        };
        base.OnStartup(e);
    }
}
