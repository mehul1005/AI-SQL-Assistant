using AiSqlAssistant.Api.Services;
using OpenAI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register OpenAI Service (Rerouted to Groq)
builder.Services.AddOpenAIService(settings => {
    settings.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("API Key is missing.");
    settings.BaseDomain = "https://api.groq.com/openai/v1"; 
});

// Register your custom service
builder.Services.AddTransient<ISqlGeneratorService, OpenAiSqlGeneratorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();