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

        // IMPORTANT: Make sure this port matches the one in your API's launch profile! (e.g., 7092)
        private readonly string _apiUrl = "https://localhost:7092/api/SqlAssistant/generate-sql";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        // Renamed method and changed return type to the full response object
        public async Task<SqlGenerationResponse> GetSqlAndDataAsync(string prompt, string schema)
        {
            try
            {
                var requestPayload = new
                {
                    UserPrompt = prompt,
                    DatabaseSchema = schema
                };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, requestPayload);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? new SqlGenerationResponse { Error = "Failed to deserialize response." };
            }
            catch (HttpRequestException ex)
            {
                return new SqlGenerationResponse { Error = $"Error connecting to API: Is the ASP.NET Core server running?\n{ex.Message}" };
            }
            catch (Exception ex)
            {
                return new SqlGenerationResponse { Error = $"An unexpected error occurred: {ex.Message}" };
            }
        }
    }
}