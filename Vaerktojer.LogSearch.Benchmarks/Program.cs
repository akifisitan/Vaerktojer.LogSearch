using System.Diagnostics;
using BenchmarkDotNet.Running;
using Vaerktojer.LogSearch.Benchmarks;

BenchmarkRunner.Run<ZipSearchBenchmark>();

Environment.Exit(0);

var sw = Stopwatch.StartNew();

await new ZipSearchBenchmark().Bench_Search_For_Pattern_In_Zip_Files_In_Directory_By_Chunking_Async();

Console.WriteLine($"Run took {sw.Elapsed.TotalSeconds} seconds.");

sw.Restart();

await new ZipSearchBenchmark().Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels();

Console.WriteLine($"Run took {sw.Elapsed.TotalSeconds} seconds.");

//new Benchmark().Bench_Search_For_Pattern_In_Zip_Files_In_Directory_Sequential();

BenchmarkRunner.Run<Benchmark>();
