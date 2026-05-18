using ECourtScraperApi.Services;
using QuestPDF.Infrastructure;

// Set QuestPDF Community license
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Register the Playwright session manager as a singleton
builder.Services.AddSingleton<PlaywrightSessionManager>();
builder.Services.AddScoped<CaseScraper>();

// Register Cache and PDF services
builder.Services.AddSingleton<ICaseCacheService, CaseCacheService>();
builder.Services.AddScoped<ICasePdfService, CasePdfService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Auto-install Playwright browsers on startup
try 
{
    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
    if (exitCode != 0) 
    {
        app.Logger.LogWarning("Playwright install exited with code {ExitCode}", exitCode);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to install Playwright browser automatically.");
}

app.Run();
