using Microsoft.EntityFrameworkCore;
using ConsolidationEngine.Data;
using ConsolidationEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=consolidation.db"));

builder.Services.AddScoped<FxTranslationService>();
builder.Services.AddScoped<EliminationService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<ConsolidationService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// --- Middleware ---

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

// seed on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Database.GetPendingMigrations().Any())
    {
        SeedData.Initialize(db);
    }
}

app.Run();
