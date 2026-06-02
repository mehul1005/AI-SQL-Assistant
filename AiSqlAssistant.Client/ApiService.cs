using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace AiSqlAssistant.Client
{
    // Replicating the new API response model
    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        // Adding the Data array to match the backend JSON
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;
        // Base URL (Make sure the port is correct!)
        private readonly string _baseUrl = "https://localhost:7092/api/SqlAssistant";

        public ApiService()
        {
            _httpClient = new HttpClient();
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

        // Phase 5: Step 2 - Execute
        public async Task<SqlGenerationResponse> ExecuteSqlAsync(string sql)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/execute", new { SqlQuery = sql });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new SqlGenerationResponse { Error = "Deserialization failed." };
            }
            catch (Exception ex) { return new SqlGenerationResponse { Error = $"API Error: {ex.Message}" }; }
        }
    }
}