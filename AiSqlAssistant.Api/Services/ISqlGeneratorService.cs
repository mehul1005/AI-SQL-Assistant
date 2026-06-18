using AiSqlAssistant.Api.Models;

namespace AiSqlAssistant.Api.Services
{
    public interface ISqlGeneratorService
    {
        Task<string> GenerateSqlAsync(string prompt, string schema);
        Task<(string Sql, string Explanation)> GenerateSqlWithExplanationAsync(string prompt, string schema);
        Task<string> GenerateSqlWithFewShotAsync(string prompt, string schema, List<QueryExample> examples);
    }
}