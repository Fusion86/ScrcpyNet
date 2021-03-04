using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpAdbClient;
using System;
using System.Reactive;

namespace ScrcpyNet.Avalonia.Sample.ViewModels
{
    public class ScrcpyViewModel : ViewModelBase
    {
        [Reactive] public bool IsConnected { get; private set; }
        [Reactive] public string DeviceName { get; private set; } = "";
        [Reactive] public Scrcpy? Scrcpy { get; private set; }

        public ReactiveCommand<DeviceData, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
        public ReactiveCommand<Unit, Unit> SendControlCommand { get; }

        public ScrcpyViewModel()
        {
            ConnectCommand = ReactiveCommand.Create<DeviceData>(Connect);
            DisconnectCommand = ReactiveCommand.Create(Disconnect);
            SendControlCommand = ReactiveCommand.Create(SendControl);
        }

        private void Connect(DeviceData device)
        {
            if (device == null) return;
            if (Scrcpy != null) throw new Exception("Already connected.");

            Scrcpy = new Scrcpy(device);
            Scrcpy.Start();
            DeviceName = Scrcpy.DeviceName;
            IsConnected = true;
        }

        private void Disconnect()
        {
            if (Scrcpy != null)
            {
                Scrcpy.Stop();
                IsConnected = false;
                Scrcpy = null;
            }
        }

        private void SendControl()
        {
            //if (Scrcpy != null)
            //    Scrcpy.SendControlCommand();
        }
    }
}
