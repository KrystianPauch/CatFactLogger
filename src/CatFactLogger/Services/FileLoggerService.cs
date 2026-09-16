using CatFactLogger.Configuration;
using CatFactLogger.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CatFactLogger.Services;

public sealed class FileLoggerService : IFileLoggerService, IDisposable
{
    private readonly string _outputFilePath;
    private readonly ILogger<FileLoggerService> _logger;

    // Async mutex - ensures only one thread writes to the file at a time
    private readonly SemaphoreSlim _writeLock = new(initialCount: 1, maxCount: 1);

    public FileLoggerService(IOptions<AppSettings> settings, ILogger<FileLoggerService> logger)
    {
        _logger = logger;
        _outputFilePath = Path.GetFullPath(settings.Value.OutputFilePath);
        EnsureDirectoryExists(_outputFilePath);

        _logger.LogInformation("Output file: {FilePath}", _outputFilePath);
    }

    public async Task AppendFactAsync(CatFact fact, CancellationToken cancellationToken = default)
    {
        var line = fact.ToLogLine(DateTime.Now) + Environment.NewLine;
        await WriteAsync(line, cancellationToken);
    }

    public async Task AppendSessionSummaryAsync(string summary, CancellationToken cancellationToken = default)
    {
        var block =
            Environment.NewLine +
            new string('=', 60) + Environment.NewLine +
            summary + Environment.NewLine +
            new string('=', 60) + Environment.NewLine;

        await WriteAsync(block, cancellationToken);
        _logger.LogInformation("Session summary saved.");
    }

    public string GetOutputFilePath() => _outputFilePath;

    private async Task WriteAsync(string content, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_outputFilePath, content, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("File write was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to file {FilePath}.", _outputFilePath);
        }
        finally
        {
            // Always release the lock, even if an exception occurred
            _writeLock.Release();
        }
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public void Dispose() => _writeLock.Dispose();
}