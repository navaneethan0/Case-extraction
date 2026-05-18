using System.Collections.Concurrent;
using ECourtScraperApi.Models;

namespace ECourtScraperApi.Services;

public interface ICaseCacheService
{
    void Set(string cnr, CaseData data);
    CaseData? Get(string cnr);
}

public class CaseCacheService : ICaseCacheService, IDisposable
{
    private readonly ConcurrentDictionary<string, (CaseData Data, DateTime CachedAt)> _cache = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<CaseCacheService> _logger;
    private readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(10);

    public CaseCacheService(ILogger<CaseCacheService> logger)
    {
        _logger = logger;
        // Start background cleanup task
        _ = StartCleanupTaskAsync(_cts.Token);
    }

    public void Set(string cnr, CaseData data)
    {
        if (string.IsNullOrWhiteSpace(cnr)) return;
        var cleanCnr = cnr.Trim().ToUpperInvariant();
        _cache[cleanCnr] = (data, DateTime.UtcNow);
        _logger.LogInformation("Cached case data for CNR: {Cnr}", cleanCnr);
    }

    public CaseData? Get(string cnr)
    {
        if (string.IsNullOrWhiteSpace(cnr)) return null;
        var cleanCnr = cnr.Trim().ToUpperInvariant();

        if (_cache.TryGetValue(cleanCnr, out var entry))
        {
            // Verify if not expired
            if (DateTime.UtcNow - entry.CachedAt <= _expirationTime)
            {
                _logger.LogInformation("Cache hit for CNR: {Cnr}", cleanCnr);
                return entry.Data;
            }
            else
            {
                _logger.LogInformation("Cache expired for CNR: {Cnr}", cleanCnr);
                _cache.TryRemove(cleanCnr, out _);
            }
        }

        _logger.LogWarning("Cache miss for CNR: {Cnr}", cleanCnr);
        return null;
    }

    private async Task StartCleanupTaskAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), token);
                var now = DateTime.UtcNow;
                foreach (var kvp in _cache)
                {
                    if (now - kvp.Value.CachedAt > _expirationTime)
                    {
                        if (_cache.TryRemove(kvp.Key, out _))
                        {
                            _logger.LogInformation("Cleaned up expired case cache for CNR: {Cnr}", kvp.Key);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CaseCacheService cleanup task");
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
