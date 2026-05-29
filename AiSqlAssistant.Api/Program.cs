using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using OpenAI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register OpenAI Service
builder.Services.AddOpenAIService(settings => {
    settings.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("API Key is missing.");
    settings.BaseDomain = "https://api.groq.com/openai/v1";
});

// Register the SQLite Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom service
builder.Services.AddTransient<ISqlGeneratorService, OpenAiSqlGeneratorService>();

var app = builder.Build();

// Create the SQLite database file and seed data automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    // Get a reference to the underlying database connection
    using var connection = dbContext.Database.GetDbConnection();
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS Applications (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AppName TEXT NOT NULL,
            CustomGroup TEXT,
            CreatedDate DATETIME
        );

        -- Only seed if the table is completely empty
        SELECT COUNT(*) FROM Applications;
    ";

    long count = (long)(command.ExecuteScalar() ?? 0);

    if (count == 0)
    {
        command.CommandText = @"
            INSERT INTO Applications (AppName, CustomGroup, CreatedDate) VALUES 
            ('Asset Priority System', 'PC', '2026-05-20 10:00:00'),
            ('CRM Dashboard', 'Sales', '2026-05-22 14:30:00'),
            ('Watchdog Monitoring App', 'PC', '2026-05-25 09:15:00'),
            ('Config Engine', 'Engineering', '2026-05-26 11:00:00'),
            ('Planning Utility', 'PC', '2026-05-28 16:45:00');
        ";
        command.ExecuteNonQuery();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();