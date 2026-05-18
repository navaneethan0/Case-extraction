using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ECourtScraperApi.Services;

public interface ICaptchaOcrService
{
    Task<string> PerformOcrAsync(byte[] imageBytes);
}

public class CaptchaOcrService : ICaptchaOcrService
{
    private readonly ILogger<CaptchaOcrService> _logger;

    public CaptchaOcrService(ILogger<CaptchaOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<string> PerformOcrAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            _logger.LogWarning("PerformOcrAsync was called with null or empty image bytes.");
            return string.Empty;
        }

        // Create unique temporary file paths inside the system's temp folder
        var tempImageFile = Path.Combine(Path.GetTempPath(), $"captcha_{Guid.NewGuid()}.png");
        var tempOutputFileBase = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}");
        var tempOutputTextFile = tempOutputFileBase + ".txt";

        try
        {
            // 1. Write the raw CAPTCHA image bytes to a temp PNG file
            await File.WriteAllBytesAsync(tempImageFile, imageBytes);

            // 2. Identify the native Tesseract executable path on macOS
            var tesseractPath = "tesseract"; // default system path
            if (File.Exists("/opt/homebrew/bin/tesseract"))
            {
                tesseractPath = "/opt/homebrew/bin/tesseract";
            }
            else if (File.Exists("/usr/local/bin/tesseract"))
            {
                tesseractPath = "/usr/local/bin/tesseract";
            }

            _logger.LogInformation("Invoking native Tesseract CLI at '{Path}'...", tesseractPath);

            // 3. Configure the process to run silently
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = tesseractPath;
            
            // PSM 7 (Page Segmentation Mode 7) treats the image as a single text line, which dramatically increases CAPTCHA accuracy.
            // Whitelist alphanumeric characters to match eCourts formatting.
            process.StartInfo.Arguments = $"\"{tempImageFile}\" \"{tempOutputFileBase}\" --psm 7 -c tessedit_char_whitelist=abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            await process.WaitForExitAsync();

            // 4. Read the recognized CAPTCHA text from Tesseract's output file
            if (File.Exists(tempOutputTextFile))
            {
                var rawText = await File.ReadAllTextAsync(tempOutputTextFile);
                var cleanedText = Regex.Replace(rawText, @"[^A-Za-z0-9]", "").Trim();
                _logger.LogInformation("CAPTCHA OCR CLI completed. Recognized suggestion: '{Text}'", cleanedText);
                return cleanedText;
            }

            _logger.LogWarning("Tesseract CLI finished but output text file was not created.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while invoking the Tesseract CLI. Ensure tesseract is installed ('brew install tesseract').");
            return string.Empty; // Fail gracefully by returning an empty string
        }
        finally
        {
            // 5. Securely clean up all temporary files
            try
            {
                if (File.Exists(tempImageFile)) File.Delete(tempImageFile);
                if (File.Exists(tempOutputTextFile)) File.Delete(tempOutputTextFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary CAPTCHA files.");
            }
        }
    }
}
