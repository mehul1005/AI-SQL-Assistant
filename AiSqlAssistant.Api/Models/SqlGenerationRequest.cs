namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationRequest
    {
        public string UserPrompt { get; set; } = string.Empty;
        // DatabaseSchema is completely removed! The client no longer needs to know about it.
    }
}