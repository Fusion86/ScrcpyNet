using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpAdbClient;
using System;
using System.Reactive;

namespace ScrcpyNet.Sample.ViewModels
{
    public class ScrcpyViewModel : ViewModelBase
    {
        [Reactive] public double BitrateKb { get; set; } = 8_000;

        [Reactive] public bool IsConnected { get; private set; }
        [Reactive] public string DeviceName { get; private set; } = "";
        [Reactive] public Scrcpy? Scrcpy { get; private set; }

        public ReactiveCommand<DeviceData, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }

        public ReactiveCommand<AndroidKeycode, Unit> SendKeycodeCommand { get; }

        public ScrcpyViewModel()
        {
            // `outputScheduler: RxApp.TaskpoolScheduler` is only needed for the WPF frontend
            // TODO: This code only works ONCE. Aka you can't reconnect after disconnecting.
            ConnectCommand = ReactiveCommand.Create<DeviceData>(Connect);
            DisconnectCommand = ReactiveCommand.Create(Disconnect);
            SendKeycodeCommand = ReactiveCommand.Create<AndroidKeycode>(SendKeycode);
        }

        private void Connect(DeviceData device)
        {
            if (device == null) return;
            if (Scrcpy != null) throw new Exception("Already connected.");

            Scrcpy = new Scrcpy(device);
            Scrcpy.Bitrate = (long)(BitrateKb * 1000);
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

        private void SendKeycode(AndroidKeycode key)
        {
            if (Scrcpy == null) return;

            Scrcpy.SendControlCommand(new KeycodeControlMessage
            {
                KeyCode = key,
                Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN
            });

            // No need to wait before sending the KeyUp event.

            Scrcpy.SendControlCommand(new KeycodeControlMessage
            {
                KeyCode = key,
                Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP
            });
        }
    }
}
