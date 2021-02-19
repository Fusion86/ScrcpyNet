using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScrcpyNet;
using SharpAdbClient;
using System;
using System.Linq;

namespace ScrcpyNet.Avalonia.Sample
{
    public class MainWindow : Window
    {
        private readonly Scrcpy? scrcpy;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            var txt = this.FindControl<TextBlock>("textBlock");
            var device = new AdbClient().GetDevices().FirstOrDefault();

            if (device == null)
            {
                txt.Text = "No device found!";
            }
            else
            {
                try
                {
                    var img = this.FindControl<Image>("image");
                    var dec = new AvaloniaScrcpy(img, txt);
                    scrcpy = new Scrcpy(device, dec);
                    scrcpy.Start();
                }
                catch (Exception ex)
                {
                    txt.Text = "Error: " + ex.Message;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            scrcpy?.Stop();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
