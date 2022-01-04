using ScrcpyNet.Sample.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ScrcpyNet.Sample.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !double.TryParse(e.Text, out _);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.Scrcpy.DisconnectCommand.Execute().Subscribe();

            base.OnClosing(e);
        }
    }
}
