using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.ReactiveUI;
using FFmpeg.AutoGen;
using Serilog;

namespace ScrcpyNet.Sample.Avalonia
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ffmpeg.RootPath = "ScrcpyNet";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ffmpeg.RootPath = "/lib/x86_64-linux-gnu/";
            }

            // Enabling debug logging completely obliterates performance
            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}
