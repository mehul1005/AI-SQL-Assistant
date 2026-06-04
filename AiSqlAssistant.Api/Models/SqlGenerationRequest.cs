namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationRequest
    {
        public string UserPrompt { get; set; } = string.Empty;
    }

    // Updated to catch the prompt and risk level from the UI!
    public class SqlExecutionRequest
    {
        public string SqlQuery { get; set; } = string.Empty;
        public string OriginalPrompt { get; set; } = "Unknown";
        public string RiskLevel { get; set; } = "UNKNOWN";
    }
}