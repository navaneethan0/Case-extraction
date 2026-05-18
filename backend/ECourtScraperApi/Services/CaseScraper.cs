using ECourtScraperApi.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ECourtScraperApi.Services;

public class CaseScraper
{
    private readonly ILogger<CaseScraper> _logger;

    public CaseScraper(ILogger<CaseScraper> logger)
    {
        _logger = logger;
    }

    public async Task<CaseDetailsResponse> ScrapeAsync(IPage page, SearchRequest request)
    {
        var response = new CaseDetailsResponse { Success = false };

        try
        {
            string? alertMessage = null;
            page.Dialog += (_, dialog) =>
            {
                alertMessage = dialog.Message;
                _ = dialog.AcceptAsync();
            };

            await page.Locator("#cino").FillAsync(request.CnrNumber);
            await page.Locator("#fcaptcha_code").FillAsync(request.Captcha);
            await page.Locator("#searchbtn").ClickAsync();

            // The site may show an error message in #errSpan, show an alert(),
            // or it may load a new view with .case_details_table
            
            var errorLocator = page.Locator("#errSpan, .alert-danger, #validateError").Filter(new() { HasTextRegex = new Regex("[a-zA-Z]") });
            var successLocator = page.Locator(".case_details_table");

            // wait for either of them, but Playwright doesn't have a simple WaitAny out-of-the-box, so we use Promise.race or polling
            for (int i = 0; i < 30; i++) // 15 seconds
            {
                await Task.Delay(500);
                
                if (alertMessage != null)
                {
                    response.Message = alertMessage;
                    return response;
                }

                var errorCount = await errorLocator.CountAsync();
                if (errorCount > 0)
                {
                    var text = await errorLocator.First.InnerTextAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        response.Message = text.Trim();
                        return response; // Success = false
                    }
                }

                var successCount = await successLocator.CountAsync();
                if (successCount > 0)
                {
                    break; // Success! It loaded the details.
                }
            }

            var finalSuccessCount = await successLocator.CountAsync();
            if (finalSuccessCount == 0 && alertMessage == null)
            {
                response.Message = "Timeout waiting for results or no case found.";
                return response;
            }

            // At this point we are looking at the case details
            var caseData = new CaseData { CnrNumber = request.CnrNumber };

            // Extract case details from tables
            // First table usually contains Case Type, Filing Number, Filing Date, Registration Number, Registration Date
            var caseDetailsTable = page.Locator(".case_details_table").First;
            if (await caseDetailsTable.CountAsync() > 0)
            {
                var text = await caseDetailsTable.InnerTextAsync();
                caseData.CaseType = ExtractPattern(text, @"Case Type\s+(.*?)\s+Filing Number");
                caseData.FilingNumber = ExtractPattern(text, @"Filing Number\s+(.*?)\s+Filing Date");
                caseData.FilingDate = ExtractPattern(text, @"Filing Date\s+(.*?)\s+Registration Number");
                caseData.RegistrationNumber = ExtractPattern(text, @"Registration Number\s+(.*?)\s+Registration Date");
                caseData.RegistrationDate = ExtractPattern(text, @"Registration Date\s+(.*?)$");
            }

            // Second table: Case Status
            var statusTable = page.Locator(".case_status_table").First;
            if (await statusTable.CountAsync() > 0)
            {
                var text = await statusTable.InnerTextAsync();
                caseData.FirstHearingDate = ExtractPattern(text, @"First Hearing Date\s+(.*?)\s+Next Hearing Date");
                caseData.NextHearingDate = ExtractPattern(text, @"Next Hearing Date\s+(.*?)\s+Case Stage");
                caseData.CaseStatus = ExtractPattern(text, @"Case Stage\s+(.*?)\s+Court Number");
                caseData.CourtEstablishment = ExtractPattern(text, @"Court Number and Judge\s+(.*?)$");
            }

            // Petitioner and Advocate
            var petitionerTable = page.Locator(".Petitioner_Advocate_table").First;
            if (await petitionerTable.CountAsync() > 0)
            {
                var text = await petitionerTable.InnerTextAsync();
                ParseParties(text, caseData.Petitioners, out var adv);
                caseData.PetitionerAdvocate = adv;
            }

            // Respondent and Advocate
            var respondentTable = page.Locator(".Respondent_Advocate_table").First;
            if (await respondentTable.CountAsync() > 0)
            {
                var text = await respondentTable.InnerTextAsync();
                ParseParties(text, caseData.Respondents, out var adv);
                caseData.RespondentAdvocate = adv;
            }

            // Acts
            var actsTable = page.Locator(".acts_table").First;
            if (await actsTable.CountAsync() > 0)
            {
                var rows = await actsTable.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 2)
                    {
                        caseData.Acts.Add($"{cells[0].Trim()} - {cells[1].Trim()}");
                    }
                }
            }

            // Hearing History
            var historyLocator = page.Locator("#history_table");
            if (await historyLocator.CountAsync() > 0)
            {
                var rows = await historyLocator.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 4)
                    {
                        caseData.Hearings.Add(new HearingHistory
                        {
                            Judge = cells[0].Trim(),
                            BusinessOnDate = cells[1].Trim(),
                            HearingDate = cells[2].Trim(),
                            Purpose = cells[3].Trim()
                        });
                    }
                }
            }

            // Order Details
            var ordersTable = page.Locator(".order_table").First;
            if (await ordersTable.CountAsync() > 0)
            {
                var rows = await ordersTable.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 2)
                    {
                        caseData.Orders.Add(new OrderDetails
                        {
                            OrderNumber = cells[0].Trim(),
                            OrderDate = cells[1].Trim()
                        });
                    }
                }
            }

            // Processes
            var processesTable = page.Locator("table").Filter(new() { HasTextRegex = new Regex("Process ID.*Process Title") }).First;
            if (await processesTable.CountAsync() > 0)
            {
                var rows = await processesTable.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 3)
                    {
                        caseData.Processes.Add(new ProcessDetail
                        {
                            ProcessId = cells[0].Trim(),
                            Title = cells[1].Trim(),
                            Date = cells[2].Trim()
                        });
                    }
                }
            }

            // Case Transfer Details
            var transferTable = page.Locator("table").Filter(new() { HasTextRegex = new Regex("Case Transfer Details within Establishment|Transfer Date") }).First;
            if (await transferTable.CountAsync() > 0)
            {
                var rows = await transferTable.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 4)
                    {
                        caseData.TransferDetails.Add(new TransferDetail
                        {
                            RegistrationNumber = cells[0].Trim(),
                            TransferDate = cells[1].Trim(),
                            FromCourt = cells[2].Trim(),
                            ToCourt = cells[3].Trim()
                        });
                    }
                }
            }

            // IA Status
            var iaTable = page.Locator("table").Filter(new() { HasTextRegex = new Regex("IA Number.*Party Name") }).First;
            if (await iaTable.CountAsync() > 0)
            {
                var rows = await iaTable.Locator("tbody tr").AllAsync();
                foreach (var row in rows)
                {
                    var cells = await row.Locator("td").AllInnerTextsAsync();
                    if (cells.Count >= 5)
                    {
                        caseData.IAStatuses.Add(new IAStatus
                        {
                            IANumber = cells[0].Trim(),
                            PartyName = cells[1].Trim(),
                            FilingDate = cells[2].Trim(),
                            NextDate = cells[3].Trim(),
                            Status = cells[4].Trim()
                        });
                    }
                }
            }

            response.Success = true;
            response.CaseDetails = caseData;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping case details.");
            response.Message = "An unexpected error occurred during scraping.";
            return response;
        }
    }

    private string ExtractPattern(string input, string pattern)
    {
        var match = Regex.Match(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private void ParseParties(string text, List<string> partyList, out string advocate)
    {
        advocate = string.Empty;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Petitioner and Advocate", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("Respondent and Advocate", StringComparison.OrdinalIgnoreCase))
                continue;
                
            if (trimmed.StartsWith("Advocate-", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("Advocate -", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("Advocate:", StringComparison.OrdinalIgnoreCase))
            {
                advocate = trimmed.Substring(trimmed.IndexOf('-') > 0 ? trimmed.IndexOf('-') + 1 : trimmed.IndexOf(':') + 1).Trim();
                continue;
            }
            
            // Match "1) Name"
            var match = Regex.Match(trimmed, @"^\d+\)\s*(.*)");
            if (match.Success)
            {
                partyList.Add(match.Groups[1].Value.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // In case it's a single unnumbered party
                if (partyList.Count == 0 && !trimmed.Contains("Advocate", StringComparison.OrdinalIgnoreCase)) 
                {
                    partyList.Add(trimmed);
                }
            }
        }
    }
}
