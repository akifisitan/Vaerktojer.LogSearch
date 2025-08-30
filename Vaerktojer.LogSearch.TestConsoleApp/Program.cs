using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;

namespace Vaerktojer.LogSearch.TestConsoleApp;

internal class Program
{
    private static readonly string[] Services =
    [
        "trade-exec",
        "risk-engine",
        "payments",
        "market-data",
        "auth",
        "reporting",
        "gateway",
    ];
    private static readonly string[] Components =
    [
        "OrderService",
        "RiskChecker",
        "SettlementProcessor",
        "QuoteStream",
        "AuthController",
        "ReportBuilder",
        "HttpClient",
        "SqlClient",
        "KafkaConsumer",
    ];
    private static readonly string[] Regions =
    [
        "us-east-1",
        "eu-west-1",
        "ap-southeast-1",
        "us-west-2",
        "eu-central-1",
    ];
    private static readonly string[] Envs = ["prod", "staging"];
    private static readonly string[] Symbols =
    [
        "AAPL",
        "MSFT",
        "GOOGL",
        "AMZN",
        "TSLA",
        "SPY",
        "ESZ4",
        "EURUSD",
        "BTCUSD",
        "ETHUSD",
        "XAUUSD",
        "NFLX",
        "NVDA",
        "META",
    ];
    private static readonly string[] Currencies = ["USD", "EUR", "GBP", "JPY"];
    private static readonly string[] Levels = ["TRACE", "DEBUG", "INFO", "WARN", "ERROR"];
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false
    );

    private static int Main(string[] args)
    {
        var opts = ParseArgs(args);

        string outputPath = opts.Get("--output", "-o") ?? "finance.log";
        int? targetLines = TryParseInt(opts.Get("--lines", "-n"));
        long? targetBytes = TryParseSize(opts.Get("--size", "-s"));
        int seed = TryParseInt(opts.Get("--seed")) ?? Random.Shared.Next();

        if (targetLines is null && targetBytes is null)
        {
            targetLines = 100_000; // sensible default
        }

        Console.WriteLine($"Generating logs -> {outputPath}");
        Console.WriteLine($"Seed: {seed}");
        Console.WriteLine(
            targetLines is not null
                ? $"Target lines: {targetLines}"
                : $"Target size: {FormatSize(targetBytes!.Value)}"
        );
        Console.WriteLine();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");

        var faker = new Faker("en");
        var rnd = new Random(seed);

        // Use a consistent newline for predictable size accounting
        using var fs = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 1 << 20
        );
        using var sw = new StreamWriter(fs, Utf8NoBom, bufferSize: 1 << 20);
        sw.NewLine = "\n";
        int newlineBytes = Utf8NoBom.GetByteCount(sw.NewLine);

        long writtenBytes = 0;
        long writtenLines = 0;

        var start = DateTimeOffset.UtcNow.AddMinutes(-faker.Random.Int(1, 120));
        var timestamp = start;

        var levelChooser = WeightedChooser(
            new[]
            {
                ("TRACE", 0.05),
                ("DEBUG", 0.10),
                ("INFO", 0.75),
                ("WARN", 0.07),
                ("ERROR", 0.03),
            },
            rnd
        );

        var eventChooser = WeightedChooser(
            new[]
            {
                ("order_placed", 0.18),
                ("order_filled", 0.14),
                ("order_rejected", 0.02),
                ("transfer_posted", 0.06),
                ("balance_update", 0.06),
                ("risk_limit_breached", 0.01),
                ("login_success", 0.10),
                ("login_failed", 0.01),
                ("quote", 0.16),
                ("price_tick", 0.12),
                ("report_generated", 0.04),
                ("heartbeat", 0.10),
            },
            rnd
        );

        var utf8 = Utf8NoBom;

        var swStopReason = "completed";

        try
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                swStopReason = "cancelled";
            };

            while (
                (targetLines is null || writtenLines < targetLines.Value)
                && (targetBytes is null || writtenBytes < targetBytes.Value)
            )
            {
                if (swStopReason == "cancelled")
                    break;

                timestamp = timestamp.AddMilliseconds(faker.Random.Int(0, 25));

                string level = levelChooser();
                string evt = eventChooser();

                var entry = GenerateLogEntry(faker, rnd, timestamp, level, evt);

                string json = JsonSerializer.Serialize(entry, JsonOpts);
                sw.WriteLine(json);

                writtenLines++;
                writtenBytes += utf8.GetByteCount(json) + newlineBytes;
            }
        }
        finally
        {
            sw.Flush();
            fs.Flush(true);
        }

        Console.WriteLine($"Done ({swStopReason}).");
        Console.WriteLine($"Lines written: {writtenLines:N0}");
        Console.WriteLine($"Bytes written: {writtenBytes:N0} ({FormatSize(writtenBytes)})");
        return 0;
    }

    private static LogEntry GenerateLogEntry(
        Faker faker,
        Random rnd,
        DateTimeOffset ts,
        string level,
        string evt
    )
    {
        string service = Pick(Services, rnd);
        string region = Pick(Regions, rnd);
        string env = Pick(Envs, rnd);
        string component = Pick(Components, rnd);

        string host = $"{service}-{region}-{faker.Random.Int(1, 4000):D4}";
        string thread = $"pool-{faker.Random.Int(1, 6)}-thread-{faker.Random.Int(1, 64)}";
        string traceId = faker.Random.AlphaNumeric(16).ToLowerInvariant();
        string spanId = faker.Random.AlphaNumeric(8).ToLowerInvariant();
        string txnId = faker.Random.Uuid().ToString();

        string accountId = $"ACC-{faker.Random.Int(100000, 999999)}";
        string userId = $"user{faker.Random.Int(10000, 99999)}";
        string currency = Pick(Currencies, rnd);
        string symbol = Pick(Symbols, rnd);
        string side = rnd.NextDouble() < 0.5 ? "BUY" : "SELL";
        decimal qty = Math.Round(faker.Random.Decimal(1, 10_000), 4);
        var price = GeneratePriceForSymbol(symbol, faker);
        decimal amount = Math.Round(qty * price, 2);

        int latency = faker.Random.Int(1, 1500);
        string ip = faker.Internet.Ip();
        string sessionId = faker.Random.Hash(16);

        // event-specific fields and message
        string message;
        int? httpStatus = null;
        string? errorType = null;
        string? errorMessage = null;

        switch (evt)
        {
            case "order_placed":
                message = $"Placed {side} {qty} {symbol} @ {price} {currency} account={accountId}";
                break;
            case "order_filled":
                decimal fillQty = Math.Round(
                    qty * Math.Clamp((decimal)(0.5 + rnd.NextDouble() / 2.0), 0.50m, 1.00m),
                    4
                );
                message = $"Filled {fillQty}/{qty} {symbol} @ {price} {currency} txn={txnId}";
                break;
            case "order_rejected":
                errorType = "OrderValidationException";
                errorMessage =
                    rnd.NextDouble() < 0.5
                        ? "Insufficient buying power"
                        : "Price out of bounds vs risk limits";
                message = $"Order rejected: {errorMessage}";
                level = "WARN";
                break;
            case "transfer_posted":
                string destAcc = $"ACC-{faker.Random.Int(100000, 999999)}";
                message =
                    $"Transfer posted {amount} {currency} from {accountId} to {destAcc} txn={txnId}";
                break;
            case "balance_update":
                decimal newBal = Math.Round(faker.Random.Decimal(500, 5_000_000), 2);
                message = $"Balance update {accountId} {currency} -> {newBal}";
                break;
            case "risk_limit_breached":
                errorType = "RiskLimitBreach";
                errorMessage = $"Max position notional exceeded for {symbol}";
                message = $"Risk check failed: {errorMessage}";
                level = "ERROR";
                break;
            case "login_success":
                httpStatus = 200;
                message = $"Login success user={userId} ip={ip} session={sessionId}";
                break;
            case "login_failed":
                httpStatus = 401;
                level = "WARN";
                message = $"Login failed user={userId} ip={ip} reason=invalid_credentials";
                break;
            case "quote":
                decimal bid = Math.Round(price - (decimal)(faker.Random.Double(0.01, 0.05)), 4);
                decimal ask = Math.Round(price + (decimal)(faker.Random.Double(0.01, 0.05)), 4);
                message = $"Quote {symbol} bid={bid} ask={ask} spread={Math.Round(ask - bid, 4)}";
                break;
            case "price_tick":
                message = $"Tick {symbol} px={price}";
                break;
            case "report_generated":
                httpStatus = 200;
                message =
                    $"Report generated type=PnL period=1D rows={faker.Random.Int(100, 50000)}";
                break;
            case "heartbeat":
                message = $"Heartbeat";
                break;
            default:
                message = $"Event {evt}";
                break;
        }

        // Occasionally add an ERROR around IO/DB/Kafka
        if (level == "ERROR" && errorType is null)
        {
            var errChoice = rnd.Next(3);
            if (errChoice == 0)
            {
                errorType = "SqlTimeoutException";
                errorMessage = "SQL timeout after 30s";
                httpStatus = 504;
            }
            if (errChoice == 1)
            {
                errorType = "KafkaCommitException";
                errorMessage = "Failed to commit offset";
            }
            if (errChoice == 2)
            {
                errorType = "HttpRequestException";
                errorMessage = "Connection reset by peer";
                httpStatus = 502;
            }
            message = $"{message} ({errorType}: {errorMessage})";
        }

        return new LogEntry
        {
            ts = ts.ToString("o"),
            level = level,
            env = env,
            region = region,
            service = service,
            component = component,
            host = host,
            thread = thread,
            traceId = traceId,
            spanId = spanId,
            @event = evt,
            message = message,
            transactionId = txnId,
            accountId = accountId,
            userId = userId,
            symbol = symbol,
            side = side,
            quantity = evt is "order_placed" or "order_filled" ? qty : null,
            price = evt is "order_placed" or "order_filled" or "price_tick" ? price : null,
            amount = evt is "transfer_posted" ? amount : null,
            currency = currency,
            ip = evt.StartsWith("login") ? ip : null,
            sessionId = evt.StartsWith("login") ? sessionId : null,
            httpStatus = httpStatus,
            latencyMs = latency,
            errorType = errorType,
            errorMessage = errorMessage,
            // a few extra operational fields useful for search benchmarks
            kafkaPartition = faker.Random.Int(0, 15),
            kafkaOffset = faker.Random.Long(0, 5_000_000),
            sqlRows = evt is "report_generated" ? faker.Random.Int(100, 50_000) : null,
        };
    }

    private static decimal GeneratePriceForSymbol(string symbol, Faker faker)
    {
        // Very rough price bands
        return symbol switch
        {
            "BTCUSD" => Math.Round(faker.Random.Decimal(20_000, 90_000), 2),
            "ETHUSD" => Math.Round(faker.Random.Decimal(800, 6_000), 2),
            "EURUSD" => Math.Round(faker.Random.Decimal(0.9m, 1.2m), 5),
            "XAUUSD" => Math.Round(faker.Random.Decimal(1500, 2600), 2),
            "SPY" => Math.Round(faker.Random.Decimal(300, 700), 2),
            "ESZ4" => Math.Round(faker.Random.Decimal(3000, 7000), 2),
            _ => Math.Round(faker.Random.Decimal(20, 1200), 2),
        };
    }

    private static T Pick<T>(IReadOnlyList<T> arr, Random rnd) => arr[rnd.Next(arr.Count)];

    private static Func<string> WeightedChooser((string key, double weight)[] items, Random rnd)
    {
        double total = 0;
        foreach (var it in items)
            total += it.weight;
        var cumulative = new double[items.Length];
        double sum = 0;
        for (int i = 0; i < items.Length; i++)
        {
            sum += items[i].weight / total;
            cumulative[i] = sum;
        }

        return () =>
        {
            double r = rnd.NextDouble();
            for (int i = 0; i < items.Length; i++)
            {
                if (r <= cumulative[i])
                    return items[i].key;
            }
            return items[^1].key;
        };
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a.StartsWith("--") || a.StartsWith("-"))
            {
                string? val = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    val = args[++i];
                }
                dict[a] = val;
            }
        }
        return dict;
    }

    private static int? TryParseInt(string? s) => int.TryParse(s, out var i) ? i : null;

    private static long? TryParseSize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        s = s.Trim().Replace("_", "").Replace(" ", "");
        // Accept plain bytes or suffixes KB, MB, GB, TB
        var numberPart = new string(s.TakeWhile(ch => char.IsDigit(ch) || ch == '.').ToArray());
        var unitPart = s[numberPart.Length..].ToUpperInvariant();

        if (
            !double.TryParse(
                numberPart,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value
            )
        )
            return null;

        long multiplier = 1;
        unitPart = unitPart switch
        {
            "" or "B" => "B",
            "K" or "KB" => "KB",
            "M" or "MB" => "MB",
            "G" or "GB" => "GB",
            "T" or "TB" => "TB",
            _ => "B",
        };

        multiplier = unitPart switch
        {
            "B" => 1L,
            "KB" => 1024L,
            "MB" => 1024L * 1024L,
            "GB" => 1024L * 1024L * 1024L,
            "TB" => 1024L * 1024L * 1024L * 1024L,
            _ => 1L,
        };
        var bytes = (long)(value * multiplier);
        return bytes < 0 ? 0 : bytes;
    }

    private static string FormatSize(long bytes)
    {
        double b = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (b >= 1024 && i < units.Length - 1)
        {
            b /= 1024;
            i++;
        }
        return $"{b:0.##} {units[i]}";
    }
}

public static class Extensions
{
    public static string? Get(this Dictionary<string, string?> d, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (d.TryGetValue(k, out var v))
                return v;
        }
        return null;
    }
}

public sealed class LogEntry
{
    public string ts { get; set; } = default!;
    public string level { get; set; } = default!;
    public string env { get; set; } = default!;
    public string region { get; set; } = default!;
    public string service { get; set; } = default!;
    public string component { get; set; } = default!;
    public string host { get; set; } = default!;
    public string thread { get; set; } = default!;
    public string traceId { get; set; } = default!;
    public string spanId { get; set; } = default!;
    public string @event { get; set; } = default!;
    public string message { get; set; } = default!;
    public string transactionId { get; set; } = default!;
    public string accountId { get; set; } = default!;
    public string userId { get; set; } = default!;
    public string symbol { get; set; } = default!;
    public string side { get; set; } = default!;
    public decimal? quantity { get; set; }
    public decimal? price { get; set; }
    public decimal? amount { get; set; }
    public string currency { get; set; } = default!;
    public string? ip { get; set; }
    public string? sessionId { get; set; }
    public int? httpStatus { get; set; }
    public int? latencyMs { get; set; }
    public string? errorType { get; set; }
    public string? errorMessage { get; set; }

    // optional operational fields
    public int? kafkaPartition { get; set; }
    public long? kafkaOffset { get; set; }
    public int? sqlRows { get; set; }
}
