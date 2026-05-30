using AiSqlAssistant.Api.Models;

namespace AiSqlAssistant.Api.Services
{
    public interface ISqlGeneratorService
    {
        Task<string> GenerateSqlAsync(string prompt, string schema);
    }
}