using System.Diagnostics;
using BenchmarkDotNet.Running;
using Vaerktojer.LogSearch.Benchmarks;

Bench();
Environment.Exit(0);
await Demo();

static async Task Demo()
{
    var sw = Stopwatch.StartNew();

    await new LogFileSearchBenchmark().Bench_Search_For_Pattern_In_Log_Files_In_Directory_By_Chunking_Async();
    await new LogFileSearchBenchmark().Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels();

    //await new ZipSearchBenchmark().Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels();

    Console.WriteLine($"Run took {sw.Elapsed.TotalSeconds} seconds.");

    sw.Restart();

    //await new ZipSearchBenchmark().Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels();

    //Console.WriteLine($"Run took {sw.Elapsed.TotalSeconds} seconds.");
}

static void Bench()
{
    //BenchmarkRunner.Run<ZipSearchBenchmark>();
    BenchmarkRunner.Run<LogFileSearchBenchmark>();
}
