using System;

namespace AiSqlAssistant.Api.Models
{
    public class QueryTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SqlTemplate { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public int UsageCount { get; set; } = 0;
    }

    public class QueryHistory
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserPrompt { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool WasExecuted { get; set; } = false;
        public string? Explanation { get; set; }
    }

    public class QueryExample
    {
        public string Prompt { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;
    }
}