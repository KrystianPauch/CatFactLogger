using CatFactLogger.Models;

namespace CatFactLogger.Services;

public interface ICatFactService
{
    // Returns null if the request fails or is cancelled
    Task<CatFact?> GetFactAsync(CancellationToken cancellationToken = default);
}