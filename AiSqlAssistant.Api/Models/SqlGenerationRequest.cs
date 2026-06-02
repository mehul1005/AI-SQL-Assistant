namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationRequest
    {
        public string UserPrompt { get; set; } = string.Empty;
    }

    // Model for the execution phase
    public class SqlExecutionRequest
    {
        public string SqlQuery { get; set; } = string.Empty;
    }
}