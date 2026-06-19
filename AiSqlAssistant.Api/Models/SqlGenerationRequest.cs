using System.Collections.Generic;

namespace AiSqlAssistant.Api.Models
{
    public class SqlGenerationRequest
    {
        public string UserPrompt { get; set; } = string.Empty;

        // New Phase 1 Features
        public List<QueryExample>? FewShotExamples { get; set; }
        public bool IncludeExplanation { get; set; } = false;
    }

    public class SqlExecutionRequest
    {
        public string SqlQuery { get; set; } = string.Empty;
        public string OriginalPrompt { get; set; } = "Unknown";
        public string RiskLevel { get; set; } = "UNKNOWN";
    }
}