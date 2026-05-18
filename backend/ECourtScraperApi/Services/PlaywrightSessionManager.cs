using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace ECourtScraperApi.Services;

public class PlaywrightSessionManager : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly ConcurrentDictionary<string, IPage> _pages = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastAccess = new();
    private readonly ILogger<PlaywrightSessionManager> _logger;

    public PlaywrightSessionManager(ILogger<PlaywrightSessionManager> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_playwright == null)
        {
            _logger.LogInformation("Initializing Playwright...");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            
            // Cleanup task
            _ = CleanupTaskAsync();
        }
    }

    private async Task CleanupTaskAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            var now = DateTime.UtcNow;
            foreach (var kvp in _lastAccess)
            {
                if (now - kvp.Value > TimeSpan.FromMinutes(5))
                {
                    _logger.LogInformation("Cleaning up idle session: {SessionId}", kvp.Key);
                    await CloseSessionAsync(kvp.Key);
                }
            }
        }
    }

    public async Task<(string SessionId, string CaptchaBase64)> GetCaptchaAsync()
    {
        if (_browser == null) await InitializeAsync();
        
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        
        // Default timeout increased for eCourts
        page.SetDefaultTimeout(60000); 
        
        await page.GotoAsync("https://services.ecourts.gov.in/ecourtindia_v6/?p=home/index");
        
        // Wait for CAPTCHA image
        var captchaElement = page.Locator("#captcha_image");
        await captchaElement.WaitForAsync();
        
        // E-courts dynamically loads captcha, small delay ensures it's fully rendered
        await Task.Delay(500); 
        var captchaBytes = await captchaElement.ScreenshotAsync();
        var base64 = Convert.ToBase64String(captchaBytes);
        
        var sessionId = Guid.NewGuid().ToString();
        _pages[sessionId] = page;
        _lastAccess[sessionId] = DateTime.UtcNow;
        
        return (sessionId, base64);
    }

    public IPage? GetPage(string sessionId)
    {
        if (_pages.TryGetValue(sessionId, out var page))
        {
            _lastAccess[sessionId] = DateTime.UtcNow;
            return page;
        }
        return null;
    }

    public async Task CloseSessionAsync(string sessionId)
    {
        if (_pages.TryRemove(sessionId, out var page))
        {
            _lastAccess.TryRemove(sessionId, out _);
            try 
            {
                await page.Context.CloseAsync();
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Error closing session context for {SessionId}", sessionId);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var key in _pages.Keys)
        {
            await CloseSessionAsync(key);
        }
        if (_browser != null) await _browser.CloseAsync();
        if (_playwright != null) _playwright.Dispose();
    }
}
