using BenchmarkDotNet.Attributes;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
internal class Benchmark
{
    [Benchmark(Baseline = true)]
    public void Bench1() { }

    [Benchmark]
    public void Bench2() { }
}
