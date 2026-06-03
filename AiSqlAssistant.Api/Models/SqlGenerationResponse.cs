namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;

        // Add this new property!
        public QueryRiskProfile RiskProfile { get; set; } = new QueryRiskProfile();

        public List<Dictionary<string, object>> Data { get; set; } = new();
    }
}