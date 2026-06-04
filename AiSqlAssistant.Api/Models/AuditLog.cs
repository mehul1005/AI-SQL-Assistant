using System;

namespace AiSqlAssistant.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserPrompt { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "UNKNOWN";
        public string Status { get; set; } = string.Empty;
        public long ExecutionDurationMs { get; set; }
        public string UserName { get; set; } = "System";
        public string ErrorMessage { get; set; } = string.Empty;
    }
}