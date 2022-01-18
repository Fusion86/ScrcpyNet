using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SharpAdbClient;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScrcpyNet.Sample.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> LoadAvailableDevicesCommand { get; }

        public ScrcpyViewModel Scrcpy { get; }
        public ObservableCollectionExtended<DeviceData> AvailableDevices { get; } = new ObservableCollectionExtended<DeviceData>();

        [Reactive] public DeviceData? SelectedDevice { get; set; }

        private static readonly ILogger log = Log.ForContext<MainWindowViewModel>();

        public MainWindowViewModel()
        {
            Scrcpy = new ScrcpyViewModel();

            LoadAvailableDevicesCommand = ReactiveCommand.Create(LoadAvailableDevices);

            Task.Run(async () =>
            {
                // Start ADB server if needed
                var srv = new AdbServer();
                if (!srv.GetStatus().IsRunning)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        srv.StartServer("ScrcpyNet/adb.exe", false);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        srv.StartServer("/usr/bin/adb", false);
                    }
                    else
                    {
                        log.Warning("Can't automatically start the ADB server on this platform.");
                    }
                }

                await LoadAvailableDevicesCommand.Execute();
            });
        }

        private void LoadAvailableDevices()
        {
            try
            {
                AvailableDevices.Load(new AdbClient().GetDevices());
            }
            catch (Exception ex)
            {
                log.Error("Couldn't load available devices", ex);
            }
        }
    }
}
