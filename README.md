# CatFactLogger

Console application built with .NET 8 that fetches cat facts from [catfact.ninja](https://catfact.ninja/fact) and saves them to a local `.txt` file.

## Requirements

- .NET 8 SDK

## How to run

```bash
dotnet run --project src/CatFactLogger
```

Press `Ctrl+C` to stop. A session summary will be saved to the output file automatically.

## Configuration

Edit `src/CatFactLogger/appsettings.json`:

| Key | Description | Default |
|---|---|---|
| `CatFactApiUrl` | API endpoint | `https://catfact.ninja/fact` |
| `OutputFilePath` | Output file path | `cat_facts.txt` |
| `FetchIntervalSeconds` | Delay between requests | `10` |
| `MaxFetchCount` | Max fetches (0 = unlimited) | `0` |
| `RetryCount` | Retry attempts on failure | `3` |
| `ShowSessionSummary` | Show summary on exit | `true` |

## Output file format
[2026-09-17 19:32:01] [len: 61] Baking chocolate is the most dangerous chocolate to your cat.
[2026-09-17 19:32:11] [len: 30] Cats sleep 70% of their lives.


## Architecture

- **Dependency Injection** – Microsoft.Extensions.Hosting
- **Polly** – automatic retry with exponential backoff (2s, 4s, 8s)
- **Graceful shutdown** – Ctrl+C saves session summary before exit
- **Unit tests** – xUnit, Moq, FluentAssertions

## Running tests

```bash
dotnet test
```