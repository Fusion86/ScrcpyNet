using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FFmpeg.AutoGen;
using System.IO;

namespace ScrcpyNet.Benchmark
{
    [SimpleJob(RuntimeMoniker.CoreRt31)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Benchmark
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            ffmpeg.RootPath = "ScrcpyNet";
        }

        [Benchmark]
        public void VideoDecode()
        {
            VideoStreamDecoder dec = new VideoStreamDecoder();
            FileStream fs = File.OpenRead(@"D:\Repos\LupoCV\src\LupoCV.CLI\bin\Debug\netcoreapp3.1\frames.avc");

            byte[] buffer = new byte[1024 * 16];

            while (fs.Read(buffer) > 0)
                dec.Decode(buffer);
        }
    }
}
