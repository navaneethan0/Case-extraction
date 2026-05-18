using Microsoft.AspNetCore.Mvc;
using ECourtScraperApi.Services;
using ECourtScraperApi.Models;

namespace ECourtScraperApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ECourtController : ControllerBase
{
    private readonly PlaywrightSessionManager _sessionManager;
    private readonly CaseScraper _scraper;
    private readonly ICaseCacheService _cacheService;
    private readonly ICasePdfService _pdfService;
    private readonly ICaptchaOcrService _ocrService;
    private readonly ILogger<ECourtController> _logger;

    public ECourtController(
        PlaywrightSessionManager sessionManager, 
        CaseScraper scraper,
        ICaseCacheService cacheService,
        ICasePdfService pdfService,
        ICaptchaOcrService ocrService,
        ILogger<ECourtController> logger)
    {
        _sessionManager = sessionManager;
        _scraper = scraper;
        _cacheService = cacheService;
        _pdfService = pdfService;
        _ocrService = ocrService;
        _logger = logger;
    }

    [HttpGet("captcha")]
    public async Task<IActionResult> GetCaptcha()
    {
        try
        {
            var result = await _sessionManager.GetCaptchaAsync();
            
            // Perform OCR on the CAPTCHA image bytes
            var captchaBytes = Convert.FromBase64String(result.CaptchaBase64);
            var ocrText = await _ocrService.PerformOcrAsync(captchaBytes);

            var response = new CaptchaResponseDto
            {
                SessionId = result.SessionId,
                CaptchaBase64 = result.CaptchaBase64,
                OcrText = ocrText
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting captcha");
            return StatusCode(500, new { error = "Failed to load captcha. Please try again." });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var page = _sessionManager.GetPage(request.SessionId);
        if (page == null)
        {
            return BadRequest(new CaseDetailsResponse { Success = false, Message = "Session expired. Please reload CAPTCHA." });
        }

        try
        {
            var response = await _scraper.ScrapeAsync(page, request);
            if (response.Success && response.CaseDetails != null)
            {
                _cacheService.Set(request.CnrNumber, response.CaseDetails);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraping failed");
            return StatusCode(500, new CaseDetailsResponse { Success = false, Message = "Failed to scrape data." });
        }
        finally
        {
            await _sessionManager.CloseSessionAsync(request.SessionId);
        }
    }

    [HttpGet("export-pdf/{cnr}")]
    public IActionResult ExportPdf(string cnr)
    {
        try
        {
            var caseData = _cacheService.Get(cnr);
            if (caseData == null)
            {
                return NotFound(new { error = $"Case data not found or session expired for CNR: {cnr}. Please search the case first before downloading the PDF." });
            }

            var pdfBytes = _pdfService.GeneratePdf(caseData);
            var safeCnr = string.Join("_", cnr.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"CaseReport_{safeCnr}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting PDF for CNR: {Cnr}", cnr);
            return StatusCode(500, new { error = "Failed to generate and download PDF case report." });
        }
    }

    [HttpPost("export-pdf")]
    public IActionResult ExportPdfDirect([FromBody] CaseData caseData)
    {
        if (caseData == null || string.IsNullOrWhiteSpace(caseData.CnrNumber))
        {
            return BadRequest(new { error = "Invalid case data provided." });
        }

        try
        {
            var pdfBytes = _pdfService.GeneratePdf(caseData);
            var safeCnr = string.Join("_", caseData.CnrNumber.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"CaseReport_{safeCnr}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error direct-exporting PDF for CNR: {Cnr}", caseData.CnrNumber);
            return StatusCode(500, new { error = "Failed to generate and download PDF case report." });
        }
    }
}
