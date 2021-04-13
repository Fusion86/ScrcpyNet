using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ScrcpyNet.Avalonia.Sample.ViewModels;
using ScrcpyNet.Avalonia.Sample.Views;

namespace ScrcpyNet.Avalonia.Sample
{
    public class App : Application
    {
        public override void Initialize()
        {
            RxApp.DefaultExceptionHandler = new RxExceptionHandler();
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
