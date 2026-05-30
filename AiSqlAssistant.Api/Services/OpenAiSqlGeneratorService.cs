using AiSqlAssistant.Api.Models;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Updated signature: takes prompt and schema as strings, returns a string
        public async Task<string> GenerateSqlAsync(string prompt, string schema)
        {
            _logger.LogInformation("Generating SQL for prompt: {Prompt}", prompt);

            // Injecting the dynamically discovered schema
            var systemMessage = $@"
                You are an expert SQL developer. 
                Generate raw, executable SQL based on the user's request.
                Do not include markdown formatting like ```sql.
                Only return the SQL query, nothing else.
                Here is the current database schema to query against:
                {schema}
            ";

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(systemMessage),
                    ChatMessage.FromUser(prompt)
                },
                Model = "llama-3.3-70b-versatile",
                Temperature = 0.1f
            });

            if (completionResult.Successful)
            {
                return completionResult.Choices.First().Message.Content?.Trim() ?? string.Empty;
            }

            _logger.LogError("OpenAI API failed: {Error}", completionResult.Error?.Message);
            throw new Exception("Failed to generate SQL.");
        }
    }
}