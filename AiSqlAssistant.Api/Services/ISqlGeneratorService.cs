using AiSqlAssistant.Api.Models;

namespace AiSqlAssistant.Api.Services
{
    public interface ISqlGeneratorService
    {
        Task<SqlGenerationResponse> GenerateSqlAsync(SqlGenerationRequest request);
    }
}