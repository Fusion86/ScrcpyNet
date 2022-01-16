using FFmpeg.AutoGen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using SharpAdbClient;
using System.IO;
using System.Linq;

namespace ScrcpyNet.Test
{
    [TestClass]
    public class UnitTest1
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _)
        {
            // HACK:
            ffmpeg.RootPath = "ScrcpyNet";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var adb = new AdbClient();
            var device = adb.GetDevices().FirstOrDefault();

            if (device == null)
            {
                Assert.Inconclusive("No device connected."); 
                return;
            }

            var adc = new Scrcpy(device);
            adc.Start();
        }

        [TestMethod]
        public void StreamDecoder()
        {
            VideoStreamDecoder dec = new VideoStreamDecoder();
            FileStream fs = File.OpenRead(@"L:\Repos\LupoCV\LupoCV.CLI\bin\Debug\netcoreapp3.1\frames.avc");

            byte[] buffer = new byte[1024 * 16];

            while (fs.Read(buffer) > 0)
                dec.Decode(buffer);
        }
    }
}
