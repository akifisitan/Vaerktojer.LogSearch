using System.Diagnostics;
using BenchmarkDotNet.Running;
using Vaerktojer.LogSearch.Benchmarks;

var sw = Stopwatch.StartNew();

//await new Benchmark().Bench_Search_For_Pattern_In_Zip_Files_In_Directory_Async();

//new Benchmark().Bench_Search_For_Pattern_In_Zip_Files_In_Directory_Sequential();

Console.WriteLine($"Run took {sw.Elapsed.TotalSeconds} seconds.");

BenchmarkRunner.Run<Benchmark>();
Environment.Exit(0);
