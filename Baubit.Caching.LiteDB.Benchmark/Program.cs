using BenchmarkDotNet.Running;

namespace Baubit.Caching.LiteDB.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<OrderedCacheBenchmarks>(args: args);
    }
}
