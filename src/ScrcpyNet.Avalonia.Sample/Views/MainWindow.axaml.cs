using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScrcpyNet.Avalonia.Sample.ViewModels;
using System;
using System.ComponentModel;

namespace ScrcpyNet.Avalonia.Sample.Views
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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is MainWindowViewModel vm)
                vm.Scrcpy.DisconnectCommand.Execute().Subscribe();
        }
    }
}
