using CatFactLogger.Models;

namespace CatFactLogger.Services;

public interface IFileLoggerService
{
    Task AppendFactAsync(CatFact fact, CancellationToken cancellationToken = default);
    Task AppendSessionSummaryAsync(string summary, CancellationToken cancellationToken = default);
    string GetOutputFilePath();
}