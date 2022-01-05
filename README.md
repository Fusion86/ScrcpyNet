# ScrcpyNet

A work in progress reimplementation of the [scrcpy client](https://github.com/Genymobile/scrcpy/tree/master/app) in C#, with support for [AvaloniaUI](https://avaloniaui.net) and WPF.

## Features

- Basic keyboard input support
- Basic touch and swiping
- Automatic screen rotation
- Window resizing
- **No** audio support
- _Usually_ crash free

## Screenshot

![Screenshot](https://i.imgur.com/yGTl9Vy.png)

## Setup

The ScrcpyNet library should automatically copy the files from the deps/{shared,win64} folder to the ScrcpyNet folder inside your bin folder.
If for some reason this doesn't happen then you can manually copy those files to a ScrcpyNet folder next to your executable.

## Usage

### WPF

Install both the `ScrcpyNet` and `ScrcpyNet.Wpf` packages from nuget.

Add the xml namespace in whatever xaml file you want to use it. In our example we are going to use MainWindow.xaml

```xml
xmlns:scrcpy="clr-namespace:ScrcpyNet.Wpf;assembly=ScrcpyNet.Wpf"
```

Now you can use the ScrcpyDisplay control. In this example we give it a name because we set the `ScrcpyDisplay.Scrcpy` property from the code-behind, but you can also use XAML bindings (as seen in the [WPF example project](https://github.com/Fusion86/ScrcpyNet/tree/master/src/ScrcpyNet.Sample.Wpf)).

```xml
<scrcpy:ScrcpyDisplay x:Name="ScrcpyDisplay"/>
```

Next we need to set the `ScrcpyDisplay.Scrcpy` property. The example below shows how to do this in the MainWindow.xaml.cs code-behind.

```cs
public MainWindow()
{
    InitializeComponent();

    // If you want logging
    // This uses the Serilog library
    //Log.Logger = new LoggerConfiguration()
    //    .MinimumLevel.Verbose()
    //    .WriteTo.Console()
    //    .WriteTo.Debug()
    //    .CreateLogger();

    // Set ffmpeg root path, the default VideoStreamDecoder needs this to be set.
    ffmpeg.RootPath = "ScrcpyNet";

    // (optional) Start ADB server if needed
    var srv = new AdbServer();
    if (!srv.GetStatus().IsRunning)
        srv.StartServer("ScrcpyNet/adb.exe", false);

    // Find connected devices
    var devices = new AdbClient().GetDevices();

    // (optional) Show message and exit when no devices are connected.
    if (devices.Count == 0)
    {
        MessageBox.Show("No device connected!");
        Close();
    }

    // Create new scrcpy instance and set it on the ScrcpyDisplay
    // NOTE: It is better to use data bindings for this.
    ScrcpyDisplay.Scrcpy = new Scrcpy(devices[0]);
    ScrcpyDisplay.Scrcpy.Start(); // Start scrcpy and start streaming
}

// Make sure to disconnect your device when closing the application, or it will hang forever.
protected override void OnClosing(CancelEventArgs e)
{
    ScrcpyDisplay.Scrcpy?.Stop();
    base.OnClosing(e);
}
```

### Avalonia

The Avalonia nuget package isn't fully function yet.

## Troubleshooting

### I can't select my device from the dropdown

Ensure that you accepted the connection on your device too. You only need to do this once per computer/phone. You can use the `adb devices` command to check if your computer can detect your device.

## Notes

This code (ab)uses `unsafe` code inside C#.

The Avalonia frontend is quite crappy, but I believe this is also because of some bugs inside Avalonia (frames don't feel 'smooth' and sometimes it crashes).

If you set the bitrate too high the videodecoder might not be able to keep up and lag. Or you'll get an timeout error which crashes the program.

## Credits

- [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) - this code is based on their client implementation, and we use their scrcpy-server.jar
- [The Android Open Source Project](https://android.googlesource.com/platform/frameworks/native/+/master/include/android) - for the input/keycodes
