using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SharpAdbClient;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ScrcpyNet.Sample.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> LoadAvailableDevicesCommand { get; }

        public ScrcpyViewModel Scrcpy { get; }
        public ObservableCollectionExtended<DeviceData> AvailableDevices { get; } = new ObservableCollectionExtended<DeviceData>();

        [Reactive] public DeviceData? SelectedDevice { get; set; }

        public MainWindowViewModel()
        {
            Scrcpy = new ScrcpyViewModel();

            LoadAvailableDevicesCommand = ReactiveCommand.Create(LoadAvailableDevices);

            Task.Run(async () =>
            {
                // Start ADB server if needed
                var srv = new AdbServer();
                if (!srv.GetStatus().IsRunning)
                    srv.StartServer("ScrcpyNet/adb.exe", false);

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
                Log.Error("Couldn't load available devices", ex);
            }
        }
    }
}
