using Avalonia.Controls;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SharpAdbClient;
using System;
using System.Reactive;

namespace ScrcpyNet.Avalonia.Sample.ViewModels
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

            LoadAvailableDevicesCommand.Execute().Subscribe();
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
