using Avalonia;
using Avalonia.ReactiveUI;
using FFmpeg.AutoGen;
using Serilog;

namespace ScrcpyNet.Avalonia.Sample
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            ffmpeg.RootPath = "ScrcpyNet";

            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Verbose()
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
