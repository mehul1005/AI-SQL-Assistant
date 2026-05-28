using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace AiSqlAssistant.Client
{
    // Replicating the API response model
    public class SqlGenerationResponse
    {
        public string GeneratedSql { get; set; } = string.Empty;
    }

    public class ApiService
    {
        // Using a single instance of HttpClient is best practice
        private readonly HttpClient _httpClient;

        // IMPORTANT: Make sure this port matches the one in your API's launch profile! (e.g., 7092)
        private readonly string _apiUrl = "https://localhost:7092/api/SqlAssistant/generate-sql";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetSqlAsync(string prompt, string schema)
        {
            try
            {
                var requestPayload = new
                {
                    UserPrompt = prompt,
                    DatabaseSchema = schema
                };

                // PostAsJsonAsync handles serialization automatically
                var response = await _httpClient.PostAsJsonAsync(_apiUrl, requestPayload);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SqlGenerationResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.GeneratedSql ?? "-- No SQL generated.";
            }
            catch (HttpRequestException ex)
            {
                return $"-- Error connecting to API: Is the ASP.NET Core server running?\n-- {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"-- An unexpected error occurred: {ex.Message}";
            }
        }
    }
}