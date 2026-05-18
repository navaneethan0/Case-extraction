using ECourtScraperApi.Services;
using ECourtScraperApi.Data;
using Microsoft.EntityFrameworkCore;
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

// Register DbContext for PostgreSQL
builder.Services.AddDbContext<CaseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Cache, PDF and OCR services
builder.Services.AddSingleton<ICaseCacheService, CaseCacheService>();
builder.Services.AddScoped<ICasePdfService, CasePdfService>();
builder.Services.AddScoped<ICaptchaOcrService, CaptchaOcrService>();

var app = builder.Build();

// Auto-migrate or create database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CaseDbContext>();
        dbContext.Database.EnsureCreated();
        app.Logger.LogInformation("PostgreSQL database initialization succeeded.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the PostgreSQL database.");
    }
}

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
