using Microsoft.Extensions.Configuration;
using CatFactLogger.Configuration;
using CatFactLogger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
    logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.local.json", optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettings>(
            context.Configuration.GetSection(AppSettings.SectionName));

        var retryCount = context.Configuration
            .GetSection(AppSettings.SectionName)
            .GetValue<int>("RetryCount", defaultValue: 3);

        // Retry policy with exponential backoff: 2s, 4s, 8s
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    Console.WriteLine($"[Polly] Retry {attempt}/{retryCount} after {timespan.TotalSeconds:F0}s");
                });

        services.AddHttpClient<ICatFactService, CatFactService>(client =>
        {
            var apiUrl = context.Configuration[$"{AppSettings.SectionName}:CatFactApiUrl"]
                         ?? "https://catfact.ninja/fact";
            client.BaseAddress = new Uri(apiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(retryPolicy);

        services.AddSingleton<IFileLoggerService, FileLoggerService>();
    })
    .Build();

// Resolve services
var catFactService = host.Services.GetRequiredService<ICatFactService>();
var fileLogger     = host.Services.GetRequiredService<IFileLoggerService>();
var settings       = host.Services.GetRequiredService<IOptions<AppSettings>>().Value;
var logger         = host.Services.GetRequiredService<ILogger<Program>>();

// Graceful shutdown on Ctrl+C
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    logger.LogInformation("Shutdown signal received.");
    cts.Cancel();
};

Console.WriteLine("CatFact Logger started. Press Ctrl+C to stop.");
Console.WriteLine($"Output file: {fileLogger.GetOutputFilePath()}");
Console.WriteLine();

// Health check
Console.Write("Checking API... ");
var healthCheck = await catFactService.GetFactAsync(cts.Token);
if (healthCheck is null)
{
    Console.WriteLine("WARNING: API unreachable. Will retry each cycle.");
}
else
{
    Console.WriteLine("OK");
    await fileLogger.AppendFactAsync(healthCheck, cts.Token);
    Console.WriteLine($"[1] {healthCheck}");
}

// Session stats
var sessionStart = DateTime.Now;
int fetchCount   = healthCheck is not null ? 1 : 0;
long totalLength = healthCheck?.Length ?? 0;

// Main loop
while (!cts.Token.IsCancellationRequested)
{
    if (settings.MaxFetchCount > 0 && fetchCount >= settings.MaxFetchCount)
    {
        logger.LogInformation("Reached maximum fetch count ({Max}).", settings.MaxFetchCount);
        break;
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(settings.FetchIntervalSeconds), cts.Token);
    }
    catch (OperationCanceledException) { break; }

    if (cts.Token.IsCancellationRequested) break;

    var fact = await catFactService.GetFactAsync(cts.Token);
    if (fact is not null)
    {
        fetchCount++;
        totalLength += fact.Length;
        await fileLogger.AppendFactAsync(fact, cts.Token);
        Console.WriteLine($"[{fetchCount}] {fact}");
    }
}

// Session summary
if (settings.ShowSessionSummary && fetchCount > 0)
{
    var duration = DateTime.Now - sessionStart;
    var avgLength = totalLength / fetchCount;

    var summary =
        $"SESSION SUMMARY{Environment.NewLine}" +
        $"  Started:       {sessionStart:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
        $"  Finished:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
        $"  Duration:      {duration:hh\\:mm\\:ss}{Environment.NewLine}" +
        $"  Facts fetched: {fetchCount}{Environment.NewLine}" +
        $"  Avg length:    {avgLength} characters";

    await fileLogger.AppendSessionSummaryAsync(summary, CancellationToken.None);

    Console.WriteLine();
    Console.WriteLine($"Facts fetched : {fetchCount}");
    Console.WriteLine($"Avg length    : {avgLength} chars");
    Console.WriteLine($"Duration      : {duration:hh\\:mm\\:ss}");
    Console.WriteLine($"File saved at : {fileLogger.GetOutputFilePath()}");
}

Console.WriteLine();
Console.WriteLine("CatFact Logger stopped.");