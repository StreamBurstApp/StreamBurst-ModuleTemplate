using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DesignerPreview
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow mw = new MainWindow();
                desktop.MainWindow = mw;
                desktop.ShutdownRequested += (_, _) => mw.ShutDown();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}