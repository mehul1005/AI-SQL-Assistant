using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace AiSqlAssistant.Client
{
    // --- Login Response Model ---
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

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

    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public QueryRiskProfile RiskProfile { get; set; } = new QueryRiskProfile();
        public string? Explanation { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
    }

    public class QueryTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SqlTemplate { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }

    public class QueryHistoryDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserPrompt { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool WasExecuted { get; set; }
        public string? Explanation { get; set; }
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // Base URLs
        //private readonly string _baseUrl = "https://localhost:7092/api/SqlAssistant";
        //private readonly string _authUrl = "https://localhost:7092/api/Auth";

        // Production Azure Cloud
        private readonly string _baseUrl = "https://aisqlassistant-api-2026-ffhgaedhabhuddbz.centralindia-01.azurewebsites.net/api/SqlAssistant";
        private readonly string _authUrl = "https://aisqlassistant-api-2026-ffhgaedhabhuddbz.centralindia-01.azurewebsites.net/api/Auth";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        // --- Login Method ---
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_authUrl}/login", new { Username = username, Password = password });

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? new LoginResponse { Error = "Deserialization failed." };
                }

                return new LoginResponse { Error = "Invalid username or password." };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Error = $"API Error: {ex.Message}" };
            }
        }

        // --- JWT Bearer Token Injection ---
        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // JWT standard requires "Bearer " prefix
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Trim()}");
            }
        }

        public async Task<SqlGenerationResponse> GenerateSqlAsync(string prompt)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/generate", new { UserPrompt = prompt });

                // Catch 401 Unauthorized errors specifically
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new SqlGenerationResponse { Error = "Session expired or unauthorized. Please log in again." };
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new SqlGenerationResponse { Error = "Deserialization failed." };
            }
            catch (Exception ex) { return new SqlGenerationResponse { Error = $"API Error: {ex.Message}" }; }
        }

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

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new SqlGenerationResponse { Error = "Session expired or unauthorized." };
                }

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

        public async Task<List<QueryHistoryDto>> GetQueryHistoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/history");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<QueryHistoryDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<QueryHistoryDto>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load history: {ex.Message}");
                return new List<QueryHistoryDto>();
            }
        }

        public async Task<List<QueryTemplateDto>> GetTemplatesAsync(string? category = null)
        {
            try
            {
                string url = string.IsNullOrWhiteSpace(category) 
                    ? $"{_baseUrl}/templates" 
                    : $"{_baseUrl}/templates?category={category}";
                    
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<QueryTemplateDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<QueryTemplateDto>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load templates: {ex.Message}");
                return new List<QueryTemplateDto>();
            }
        }

        public async Task<SqlGenerationResponse> GenerateSqlWithExplanationAsync(string prompt)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/generate", new { 
                    UserPrompt = prompt,
                    IncludeExplanation = true
                });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new SqlGenerationResponse { Error = "Session expired or unauthorized." };
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new SqlGenerationResponse { Error = "Deserialization failed." };
            }
            catch (Exception ex) { return new SqlGenerationResponse { Error = $"API Error: {ex.Message}" }; }
        }
    }
}