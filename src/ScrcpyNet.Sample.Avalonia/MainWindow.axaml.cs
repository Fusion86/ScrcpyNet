using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScrcpyNet.Sample.ViewModels;
using System;

namespace ScrcpyNet.Sample.Avalonia
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is MainWindowViewModel vm)
                vm.Scrcpy.DisconnectCommand.Execute().Subscribe();
        }
    }
}
