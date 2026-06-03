namespace AiSqlAssistant.Api.Models
{
    public class QueryRiskProfile
    {
        public string RiskLevel { get; set; } = "LOW";
        public string Operation { get; set; } = "UNKNOWN";
        public List<string> AffectedTables { get; set; } = new List<string>();
        public bool IsExecutionBlocked { get; set; } = false;
    }
}