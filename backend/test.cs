using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        Console.WriteLine("Navigating...");
        await page.GotoAsync("https://services.ecourts.gov.in/ecourtindia_v6/?p=home/index");
        Console.WriteLine("Taking screenshot...");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png" });
        Console.WriteLine("Done.");
        await browser.CloseAsync();
    }
}
