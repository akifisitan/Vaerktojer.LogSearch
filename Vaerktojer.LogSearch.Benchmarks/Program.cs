using BenchmarkDotNet.Running;
using Vaerktojer.LogSearch.Benchmarks;

var v = new Benchmark();

await v.BenchAsync();

Environment.Exit(0);

BenchmarkRunner.Run<Benchmark>();
