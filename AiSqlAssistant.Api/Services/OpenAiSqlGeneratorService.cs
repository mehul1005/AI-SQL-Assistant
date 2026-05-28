using AiSqlAssistant.Api.Models;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;

namespace AiSqlAssistant.Api.Services
{
    public class OpenAiSqlGeneratorService : ISqlGeneratorService
    {
        private readonly IOpenAIService _openAiService;
        private readonly ILogger<OpenAiSqlGeneratorService> _logger;

        public OpenAiSqlGeneratorService(IOpenAIService openAiService, ILogger<OpenAiSqlGeneratorService> logger)
        {
            _openAiService = openAiService;
            _logger = logger;
        }

        public async Task<SqlGenerationResponse> GenerateSqlAsync(SqlGenerationRequest request)
        {
            _logger.LogInformation("Generating SQL for prompt: {Prompt}", request.UserPrompt);

            var systemMessage = $@"
                You are an expert SQL Server developer. 
                Generate raw, executable T-SQL based on the user's request.
                Do not include markdown formatting like ```sql.
                Only return the SQL query, nothing else.
                Here is the current database schema to query against:
                {request.DatabaseSchema}
            ";

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(systemMessage),
                    ChatMessage.FromUser(request.UserPrompt)
                },
                Model = "llama-3.3-70b-versatile",
                Temperature = 0.1f
            });

            if (completionResult.Successful)
            {
                var rawSql = completionResult.Choices.First().Message.Content;
                return new SqlGenerationResponse { GeneratedSql = rawSql!.Trim() };
            }

            _logger.LogError("OpenAI API failed: {Error}", completionResult.Error?.Message);
            throw new Exception("Failed to generate SQL.");
        }
    }
}