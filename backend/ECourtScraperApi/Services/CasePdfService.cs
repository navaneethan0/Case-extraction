using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ECourtScraperApi.Models;

namespace ECourtScraperApi.Services;

public interface ICasePdfService
{
    byte[] GeneratePdf(CaseData caseData);
}

public class CasePdfService : ICasePdfService
{
    public byte[] GeneratePdf(CaseData caseData)
    {
        var document = new CaseReportDocument(caseData);
        return document.GeneratePdf();
    }
}

internal class CaseReportDocument : IDocument
{
    private readonly CaseData _model;
    
    // Theme Colors (Deep Navy and Steel Grey/Slate as static Color objects)
    private static readonly Color PrimaryColor = Color.FromHex("#1A365D"); // Deep Navy
    private static readonly Color SecondaryColor = Color.FromHex("#4A5568"); // Slate Grey
    private static readonly Color LightBackground = Color.FromHex("#F7FAFC"); // Soft White/Gray
    private static readonly Color BorderColor = Color.FromHex("#E2E8F0"); // Very Light Grey

    public CaseReportDocument(CaseData model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36); // 0.5 inch margins are perfect for data-rich reports
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("eCourts Case Report").FontSize(20).Bold().FontColor(PrimaryColor);
                column.Item().Text($"Generated on: {DateTime.Now:dd MMM yyyy, hh:mm tt}").FontSize(8).FontColor(SecondaryColor);
            });

            // Modern typographic badge logo on right
            row.ConstantItem(100).AlignRight().AlignMiddle().Column(col => 
            {
                col.Item()
                   .Border(1.5f)
                   .BorderColor(PrimaryColor)
                   .Background(LightBackground)
                   .PaddingHorizontal(8)
                   .PaddingVertical(4)
                   .AlignCenter()
                   .Text("eCOURTS SYSTEM")
                   .FontSize(8)
                   .Bold()
                   .FontColor(PrimaryColor);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            // 1. Basic Information Section
            column.Item().Element(ComposeBasicInfo);

            // 2. Case Status Section
            column.Item().Element(ComposeCaseStatus);

            // 3. Parties & Advocates Section
            column.Item().Element(ComposeParties);

            // 4. Hearing History Table
            if (_model.Hearings != null && _model.Hearings.Any())
            {
                column.Item().Element(ComposeHearingHistory);
            }

            // 5. Orders / Judgments
            if (_model.Orders != null && _model.Orders.Any())
            {
                column.Item().Element(ComposeOrders);
            }
            
            // 6. Acts / Relevant Sections
            if (_model.Acts != null && _model.Acts.Any())
            {
                column.Item().Element(ComposeActs);
            }
        });
    }

    private void ComposeBasicInfo(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "1. CASE BASIC INFORMATION"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("CNR Number: ").Bold(); t.Span(_model.CnrNumber); });
                        c.Item().Text(t => { t.Span("Case Title: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.CaseTitle) ? "N/A" : _model.CaseTitle); });
                        c.Item().Text(t => { t.Span("Case Type: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.CaseType) ? "N/A" : _model.CaseType); });
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Filing Number: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.FilingNumber) ? "N/A" : _model.FilingNumber); });
                        c.Item().Text(t => { t.Span("Filing Date: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.FilingDate) ? "N/A" : _model.FilingDate); });
                        c.Item().Text(t => { t.Span("Registration Number: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.RegistrationNumber) ? "N/A" : _model.RegistrationNumber); });
                        c.Item().Text(t => { t.Span("Registration Date: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.RegistrationDate) ? "N/A" : _model.RegistrationDate); });
                    });
                });
            });
        });
    }

    private void ComposeCaseStatus(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "2. CASE STATUS & COURT DETAILS"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(Colors.White).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("First Hearing Date: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.FirstHearingDate) ? "N/A" : _model.FirstHearingDate); });
                        c.Item().Text(t => { t.Span("Next Hearing Date: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.NextHearingDate) ? "N/A" : _model.NextHearingDate); });
                        c.Item().Text(t => { t.Span("Case Stage: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.CaseStatus) ? "N/A" : _model.CaseStatus); });
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Court Establishment: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.CourtEstablishment) ? "N/A" : _model.CourtEstablishment); });
                        c.Item().Text(t => { t.Span("Judge Name: ").Bold(); t.Span(string.IsNullOrWhiteSpace(_model.Judge) ? "N/A" : _model.Judge); });
                    });
                });
            });
        });
    }

    private void ComposeParties(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "3. PARTIES & ADVOCATES"));
            
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
                {
                    c.Spacing(3);
                    c.Item().Text("Petitioner(s)").Bold().FontColor(PrimaryColor).Underline();
                    foreach (var pet in _model.Petitioners)
                    {
                        c.Item().Text($"• {pet}").FontSize(8.5f);
                    }
                    if (!_model.Petitioners.Any())
                    {
                        c.Item().Text("• N/A");
                    }
                    c.Item().PaddingTop(2).Text(t => 
                    {
                        t.DefaultTextStyle(x => x.FontSize(8.5f));
                        t.Span("Advocate: ").Bold(); 
                        t.Span(string.IsNullOrWhiteSpace(_model.PetitionerAdvocate) ? "N/A" : _model.PetitionerAdvocate); 
                    });
                });
                
                row.ConstantItem(10); // Column Spacer
                
                row.RelativeItem().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
                {
                    c.Spacing(3);
                    c.Item().Text("Respondent(s)").Bold().FontColor(PrimaryColor).Underline();
                    foreach (var resp in _model.Respondents)
                    {
                        c.Item().Text($"• {resp}").FontSize(8.5f);
                    }
                    if (!_model.Respondents.Any())
                    {
                        c.Item().Text("• N/A");
                    }
                    c.Item().PaddingTop(2).Text(t => 
                    {
                        t.DefaultTextStyle(x => x.FontSize(8.5f));
                        t.Span("Advocate: ").Bold(); 
                        t.Span(string.IsNullOrWhiteSpace(_model.RespondentAdvocate) ? "N/A" : _model.RespondentAdvocate); 
                    });
                });
            });
        });
    }

    private void ComposeHearingHistory(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "4. HEARING HISTORY"));
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn(2);   // Purpose
                    columns.RelativeColumn(2);   // Judge
                    columns.RelativeColumn(3);   // Business
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Hearing Date").Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Purpose").Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Judge").Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Business").Bold().FontColor(Colors.White);

                    static IContainer HeaderStyle(IContainer container) => 
                        container.Background(PrimaryColor).Padding(5).AlignLeft();
                });

                int i = 0;
                foreach (var hearing in _model.Hearings)
                {
                    var bg = i % 2 == 0 ? Colors.White : LightBackground;

                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.HearingDate) ? "N/A" : hearing.HearingDate);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.Purpose) ? "N/A" : hearing.Purpose);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.Judge) ? "N/A" : hearing.Judge);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.BusinessOnDate) ? "N/A" : hearing.BusinessOnDate);

                    i++;

                    IContainer CellStyle(IContainer container) => 
                        container.Background(bg).BorderBottom(1).BorderColor(BorderColor).Padding(5).AlignLeft();
                }
            });
        });
    }

    private void ComposeOrders(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "5. ORDERS / JUDGMENTS"));
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120); // Date
                    columns.RelativeColumn();    // Order Number/Details
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Order Date").Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Order Number / Details").Bold().FontColor(Colors.White);

                    static IContainer HeaderStyle(IContainer container) => 
                        container.Background(SecondaryColor).Padding(5).AlignLeft();
                });

                int i = 0;
                foreach (var order in _model.Orders)
                {
                    var bg = i % 2 == 0 ? Colors.White : LightBackground;

                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(order.OrderDate) ? "N/A" : order.OrderDate);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(order.OrderNumber) ? "N/A" : order.OrderNumber);

                    i++;

                    IContainer CellStyle(IContainer container) => 
                        container.Background(bg).BorderBottom(1).BorderColor(BorderColor).Padding(5).AlignLeft();
                }
            });
        });
    }
    
    private void ComposeActs(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(title => SectionTitle(title, "6. RELEVANT ACTS"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
            {
                foreach (var act in _model.Acts)
                {
                    c.Item().Text($"• {act}").FontSize(8.5f);
                }
            });
        });
    }

    private void SectionTitle(IContainer container, string text)
    {
        container.BorderBottom(1.5f).BorderColor(PrimaryColor).PaddingBottom(3).PaddingTop(5).Text(text).Bold().FontSize(10).FontColor(PrimaryColor);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("Confidential Case Report - Generated by eCourts Automated Scraper System").Italic().FontSize(7).FontColor(SecondaryColor);
            });
            
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Page ").FontSize(7).FontColor(SecondaryColor);
                t.CurrentPageNumber().FontSize(7).FontColor(SecondaryColor);
                t.Span(" of ").FontSize(7).FontColor(SecondaryColor);
                t.TotalPages().FontSize(7).FontColor(SecondaryColor);
            });
        });
    }
}
