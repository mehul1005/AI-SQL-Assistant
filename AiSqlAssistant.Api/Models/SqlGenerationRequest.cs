namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationRequest
    {
        public string UserPrompt { get; set; } = string.Empty;
        public string DatabaseSchema { get; set; } = string.Empty;
    }
}