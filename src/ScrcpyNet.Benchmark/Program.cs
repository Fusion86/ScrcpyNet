using BenchmarkDotNet.Running;

namespace ScrcpyNet.Benchmark
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
