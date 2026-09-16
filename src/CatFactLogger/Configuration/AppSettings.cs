namespace CatFactLogger.Configuration;

public sealed class AppSettings
{
    public const string SectionName = "AppSettings";

    public string CatFactApiUrl { get; init; } = "https://catfact.ninja/fact";
    public string OutputFilePath { get; init; } = "cat_facts.txt";
    public int FetchIntervalSeconds { get; init; } = 10;

    // 0 = unlimited, runs until Ctrl+C
    public int MaxFetchCount { get; init; } = 0;

    public int RetryCount { get; init; } = 3;
    public bool ShowSessionSummary { get; init; } = true;
}