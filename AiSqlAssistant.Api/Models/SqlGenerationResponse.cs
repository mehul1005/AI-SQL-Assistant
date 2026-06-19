using System.Collections.Generic;

namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public QueryRiskProfile RiskProfile { get; set; } = new QueryRiskProfile();

        // New Phase 1 Feature
        public string? Explanation { get; set; }

        public List<Dictionary<string, object>> Data { get; set; } = new();
    }
}