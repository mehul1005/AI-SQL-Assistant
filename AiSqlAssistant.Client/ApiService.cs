using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace AiSqlAssistant.Client
{
    // 0. Added the Audit Log model
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserPrompt { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long ExecutionDurationMs { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // 1. Add the Risk Profile model to the client
    public class QueryRiskProfile
    {
        public string RiskLevel { get; set; } = "LOW";
        public string Operation { get; set; } = "UNKNOWN";
        public List<string> AffectedTables { get; set; } = new List<string>();
        public bool IsExecutionBlocked { get; set; } = false;
    }

    public class AnalyticsSummary
    {
        public int TotalQueries { get; set; }
        public int BlockedQueries { get; set; }
        public int CriticalRisks { get; set; }
        public double AverageDurationMs { get; set; }
        public List<UserActivity> TopUsers { get; set; } = new();
    }

    public class UserActivity
    {
        public string UserName { get; set; } = string.Empty;
        public int QueryCount { get; set; }
    }

    // 2. Update the Response model to include it
    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public QueryRiskProfile RiskProfile { get; set; } = new QueryRiskProfile(); // Added!
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;

        //private readonly string _baseUrl = "https://localhost:7092/api/SqlAssistant";

        private readonly string _baseUrl = "https://aisqlassistant-api-2026-ffhgaedhabhuddbz.centralindia-01.azurewebsites.net/api/SqlAssistant";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        // --- Set API Key METHOD ---
        public void SetApiKey(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());
            }
        }

        // Phase 5: Step 1 - Generate
        public async Task<SqlGenerationResponse> GenerateSqlAsync(string prompt)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/generate", new { UserPrompt = prompt });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new SqlGenerationResponse { Error = "Deserialization failed." };
            }
            catch (Exception ex) { return new SqlGenerationResponse { Error = $"API Error: {ex.Message}" }; }
        }

        // Phase 5/7: Step 2 - Execute (Now passing audit context)
        public async Task<SqlGenerationResponse> ExecuteSqlAsync(string sql, string originalPrompt, string riskLevel)
        {
            try
            {
                var payload = new
                {
                    SqlQuery = sql,
                    OriginalPrompt = originalPrompt,
                    RiskLevel = riskLevel
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/execute", payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new SqlGenerationResponse { Error = "Deserialization failed." };
            }
            catch (Exception ex) { return new SqlGenerationResponse { Error = $"API Error: {ex.Message}" }; }
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/audit-logs");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<AuditLog>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<AuditLog>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load logs: {ex.Message}");
                return new List<AuditLog>();
            }
        }

        public async Task<AnalyticsSummary?> GetAnalyticsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/analytics");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AnalyticsSummary>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
    }
}