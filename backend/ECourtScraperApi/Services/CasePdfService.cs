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
    
    // Theme Colors (Deep Navy and Slate Grey as static Color objects)
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
            row.RelativeItem().AlignMiddle().Column(column =>
            {
                column.Item().Text("eCourts Case Report").FontSize(22).Bold().FontColor(PrimaryColor);
                column.Item().PaddingTop(2).Text($"Generated on: {DateTime.Now:dd MMM yyyy, hh:mm tt}").FontSize(8.5f).FontColor(SecondaryColor);
            });

            // Modern typographic badge logo on right
            row.ConstantItem(115)
               .AlignRight()
               .AlignMiddle()
               .Border(1.5f)
               .BorderColor(PrimaryColor)
               .Background(LightBackground)
               .PaddingHorizontal(8)
               .PaddingVertical(4)
               .AlignCenter()
               .Text("eCOURTS SYSTEM")
               .FontSize(8.5f)
               .Bold()
               .FontColor(PrimaryColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(18);

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

    private void ComposeLabelValue(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(110).Text(label).Bold().FontSize(8.5f).FontColor(SecondaryColor);
            row.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "N/A" : value).FontSize(8.5f).FontColor(Colors.Black).LineHeight(1.2f);
        });
    }

    private void ComposeBasicInfo(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(title => SectionTitle(title, "1. CASE BASIC INFORMATION"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Spacing(4);
                        ComposeLabelValue(c, "CNR Number:", _model.CnrNumber);
                        ComposeLabelValue(c, "Case Title:", _model.CaseTitle);
                        ComposeLabelValue(c, "Case Type:", _model.CaseType);
                    });
                    
                    row.ConstantItem(15); // Spacer
                    
                    row.RelativeItem().Column(c =>
                    {
                        c.Spacing(4);
                        ComposeLabelValue(c, "Filing Number:", _model.FilingNumber);
                        ComposeLabelValue(c, "Filing Date:", _model.FilingDate);
                        ComposeLabelValue(c, "Registration Number:", _model.RegistrationNumber);
                        ComposeLabelValue(c, "Registration Date:", _model.RegistrationDate);
                    });
                });
            });
        });
    }

    private void ComposeCaseStatus(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(title => SectionTitle(title, "2. CASE STATUS & COURT DETAILS"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(Colors.White).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Spacing(4);
                        ComposeLabelValue(c, "First Hearing Date:", _model.FirstHearingDate);
                        ComposeLabelValue(c, "Next Hearing Date:", _model.NextHearingDate);
                        ComposeLabelValue(c, "Case Stage:", _model.CaseStatus);
                    });
                    
                    row.ConstantItem(15); // Spacer
                    
                    row.RelativeItem().Column(c =>
                    {
                        c.Spacing(4);
                        ComposeLabelValue(c, "Court Establishment:", _model.CourtEstablishment);
                        ComposeLabelValue(c, "Judge Name:", _model.Judge);
                    });
                });
            });
        });
    }

    private void ComposeParties(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(title => SectionTitle(title, "3. PARTIES & ADVOCATES"));
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(12); // Spacer between columns
                    columns.RelativeColumn();
                });

                // Petitioner Card
                table.Cell().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
                {
                    c.Spacing(4);
                    c.Item().Text("Petitioner(s)").Bold().FontSize(9).FontColor(PrimaryColor);
                    
                    foreach (var pet in _model.Petitioners)
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(10).Text("•").FontSize(8.5f).FontColor(PrimaryColor);
                            r.RelativeItem().Text(pet).FontSize(8.5f).LineHeight(1.2f);
                        });
                    }
                    if (!_model.Petitioners.Any())
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(10).Text("•").FontSize(8.5f).FontColor(PrimaryColor);
                            r.RelativeItem().Text("N/A").FontSize(8.5f).LineHeight(1.2f);
                        });
                    }
                    
                    c.Item().PaddingTop(4).Text(t => 
                    {
                        t.DefaultTextStyle(x => x.FontSize(8.5f));
                        t.Span("Advocate: ").Bold().FontColor(SecondaryColor); 
                        t.Span(string.IsNullOrWhiteSpace(_model.PetitionerAdvocate) ? "N/A" : _model.PetitionerAdvocate); 
                    });
                });

                // Spacer Cell
                table.Cell();

                // Respondent Card
                table.Cell().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
                {
                    c.Spacing(4);
                    c.Item().Text("Respondent(s)").Bold().FontSize(9).FontColor(PrimaryColor);
                    
                    foreach (var resp in _model.Respondents)
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(10).Text("•").FontSize(8.5f).FontColor(PrimaryColor);
                            r.RelativeItem().Text(resp).FontSize(8.5f).LineHeight(1.2f);
                        });
                    }
                    if (!_model.Respondents.Any())
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(10).Text("•").FontSize(8.5f).FontColor(PrimaryColor);
                            r.RelativeItem().Text("N/A").FontSize(8.5f).LineHeight(1.2f);
                        });
                    }
                    
                    c.Item().PaddingTop(4).Text(t => 
                    {
                        t.DefaultTextStyle(x => x.FontSize(8.5f));
                        t.Span("Advocate: ").Bold().FontColor(SecondaryColor); 
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
            column.Spacing(6);
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
                    header.Cell().Element(HeaderStyle).Text("Hearing Date").Bold().FontSize(8.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Purpose").Bold().FontSize(8.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Judge").Bold().FontSize(8.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Business").Bold().FontSize(8.5f).FontColor(Colors.White);

                    static IContainer HeaderStyle(IContainer container) => 
                        container.Background(PrimaryColor).PaddingHorizontal(8).PaddingVertical(6).AlignLeft();
                });

                int i = 0;
                foreach (var hearing in _model.Hearings)
                {
                    var bg = i % 2 == 0 ? Colors.White : LightBackground;

                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.HearingDate) ? "N/A" : hearing.HearingDate).FontSize(8.5f).LineHeight(1.15f);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.Purpose) ? "N/A" : hearing.Purpose).FontSize(8.5f).LineHeight(1.15f);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.Judge) ? "N/A" : hearing.Judge).FontSize(8.5f).LineHeight(1.15f);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(hearing.BusinessOnDate) ? "N/A" : hearing.BusinessOnDate).FontSize(8.5f).LineHeight(1.15f);

                    i++;

                    IContainer CellStyle(IContainer container) => 
                        container.Background(bg).BorderBottom(1).BorderColor(BorderColor).PaddingHorizontal(8).PaddingVertical(6).AlignLeft();
                }
            });
        });
    }

    private void ComposeOrders(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
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
                    header.Cell().Element(HeaderStyle).Text("Order Date").Bold().FontSize(8.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderStyle).Text("Order Number / Details").Bold().FontSize(8.5f).FontColor(Colors.White);

                    static IContainer HeaderStyle(IContainer container) => 
                        container.Background(SecondaryColor).PaddingHorizontal(8).PaddingVertical(6).AlignLeft();
                });

                int i = 0;
                foreach (var order in _model.Orders)
                {
                    var bg = i % 2 == 0 ? Colors.White : LightBackground;

                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(order.OrderDate) ? "N/A" : order.OrderDate).FontSize(8.5f).LineHeight(1.15f);
                    table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(order.OrderNumber) ? "N/A" : order.OrderNumber).FontSize(8.5f).LineHeight(1.15f);

                    i++;

                    IContainer CellStyle(IContainer container) => 
                        container.Background(bg).BorderBottom(1).BorderColor(BorderColor).PaddingHorizontal(8).PaddingVertical(6).AlignLeft();
                }
            });
        });
    }
    
    private void ComposeActs(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(title => SectionTitle(title, "6. RELEVANT ACTS"));
            
            column.Item().Border(1).BorderColor(BorderColor).Background(LightBackground).Padding(10).Column(c =>
            {
                c.Spacing(4);
                foreach (var act in _model.Acts)
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(10).Text("•").FontSize(8.5f).FontColor(PrimaryColor);
                        r.RelativeItem().Text(act).FontSize(8.5f).LineHeight(1.2f);
                    });
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
        container.BorderTop(0.5f).BorderColor(BorderColor).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().AlignLeft().AlignMiddle().Text(t =>
            {
                t.Span("Confidential Case Report - Generated by eCourts Automated Scraper System").Italic().FontSize(7.5f).FontColor(SecondaryColor);
            });
            
            row.RelativeItem().AlignRight().AlignMiddle().Text(t =>
            {
                t.Span("Page ").FontSize(7.5f).FontColor(SecondaryColor);
                t.CurrentPageNumber().FontSize(7.5f).FontColor(SecondaryColor);
                t.Span(" of ").FontSize(7.5f).FontColor(SecondaryColor);
                t.TotalPages().FontSize(7.5f).FontColor(SecondaryColor);
            });
        });
    }
}
