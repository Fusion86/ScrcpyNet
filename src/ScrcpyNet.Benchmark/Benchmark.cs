using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FFmpeg.AutoGen;
using System.IO;

namespace ScrcpyNet.Benchmark
{
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    [SimpleJob(RuntimeMoniker.CoreRt31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class Benchmark
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            ffmpeg.RootPath = "L:/Sources/ffmpeg-4.3.2-2021-02-02-full_build-shared/bin";
        }

        [Benchmark]
        public void Stuff()
        {
            VideoStreamDecoder dec = new VideoStreamDecoder();
            FileStream fs = File.OpenRead(@"L:\Repos\LupoCV\LupoCV.CLI\bin\Debug\netcoreapp3.1\frames.avc");

            byte[] buffer = new byte[1024 * 16];

            while (fs.Read(buffer) > 0)
                dec.Decode(buffer);
        }
    }
}
