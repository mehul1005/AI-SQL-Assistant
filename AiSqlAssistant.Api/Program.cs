using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenAI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- START APPLICATION INSIGHTS TELEMETRY ---
builder.Services.AddApplicationInsightsTelemetry();

// --- JWT AUTHENTICATION SETUP ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// 1. REGISTER AZURE SQL INSTEAD OF SQLITE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register OpenAI Service
builder.Services.AddOpenAIService(settings => {
    settings.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("API Key is missing.");
    settings.BaseDomain = "https://api.groq.com/openai/v1";
});

// Register custom service
builder.Services.AddTransient<ISqlGeneratorService, OpenAiSqlGeneratorService>();

// Register custom service
builder.Services.AddTransient<ISqlGeneratorService, OpenAiSqlGeneratorService>();

var app = builder.Build();

// 2. AZURE SQL CLOUD STARTUP SCRIPT
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // WARNING: EnsureDeleted is GONE. We never wipe a production Azure SQL database!
    dbContext.Database.EnsureCreated();

    using var connection = dbContext.Database.GetDbConnection();
    connection.Open();

    using var command = connection.CreateCommand();

    // 3. UPDATED FOR T-SQL SYNTAX WITH AUDIT LOGS
    command.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Applications')
        BEGIN
            CREATE TABLE Applications (
                Id INT PRIMARY KEY IDENTITY(1,1),
                AppName NVARCHAR(255) NOT NULL,
                CustomGroup NVARCHAR(100),
                CreatedDate DATETIME
            );
        END;

        -- NEW: Force SQL Server to build the AuditLogs table!
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
        BEGIN
            CREATE TABLE AuditLogs (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Timestamp DATETIME2 NOT NULL,
                UserPrompt NVARCHAR(MAX) NOT NULL,
                GeneratedSql NVARCHAR(MAX) NOT NULL,
                RiskLevel NVARCHAR(MAX) NOT NULL,
                Status NVARCHAR(MAX) NOT NULL,
                ExecutionDurationMs BIGINT NOT NULL,
                UserName NVARCHAR(MAX) NOT NULL,
                ErrorMessage NVARCHAR(MAX) NOT NULL
            );
        END;

        SELECT COUNT(*) FROM Applications;
    ";

    long count = Convert.ToInt64(command.ExecuteScalar() ?? 0);

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

app.UseAuthentication(); // Must come before Authorization
app.UseAuthorization();

app.MapControllers();
app.Run();