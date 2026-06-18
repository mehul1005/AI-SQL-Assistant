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

        public async Task<string> GenerateSqlAsync(string prompt, string schema)
        {
            _logger.LogInformation("Generating SQL for prompt: {Prompt}", prompt);

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

        public async Task<(string Sql, string Explanation)> GenerateSqlWithExplanationAsync(string prompt, string schema)
        {
            _logger.LogInformation("Generating SQL with explanation for prompt: {Prompt}", prompt);

            var systemMessage = $@"
                You are an expert SQL developer and teacher.
                Generate raw, executable SQL based on the user's request AND provide a clear explanation.
                
                IMPORTANT: Return your response in this EXACT format:
                ---SQL_START---
                [your SQL query here, no markdown]
                ---SQL_END---
                ---EXPLANATION_START---
                [your plain English explanation here]
                ---EXPLANATION_END---
                
                Database schema:
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
                var content = completionResult.Choices.First().Message.Content?.Trim() ?? string.Empty;
                return ParseSqlAndExplanation(content);
            }

            _logger.LogError("OpenAI API failed: {Error}", completionResult.Error?.Message);
            throw new Exception("Failed to generate SQL with explanation.");
        }

        public async Task<string> GenerateSqlWithFewShotAsync(string prompt, string schema, List<Models.QueryExample> examples)
        {
            _logger.LogInformation("Generating SQL with few-shot examples for prompt: {Prompt}", prompt);

            var systemMessage = $@"
                You are an expert SQL developer.
                Generate raw, executable SQL based on the user's request.
                Learn from the provided examples and follow similar patterns.
                Do not include markdown formatting like ```sql.
                Only return the SQL query, nothing else.
                
                Database schema:
                {schema}
            ";

            var messages = new List<ChatMessage>();
            messages.Add(ChatMessage.FromSystem(systemMessage));

            // Add few-shot examples
            foreach (var example in examples)
            {
                messages.Add(ChatMessage.FromUser($"Prompt: {example.Prompt}"));
                messages.Add(ChatMessage.FromAssistant(example.Sql));
            }

            // Add actual user prompt
            messages.Add(ChatMessage.FromUser(prompt));

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = "llama-3.3-70b-versatile",
                Temperature = 0.1f
            });

            if (completionResult.Successful)
            {
                return completionResult.Choices.First().Message.Content?.Trim() ?? string.Empty;
            }

            _logger.LogError("OpenAI API failed: {Error}", completionResult.Error?.Message);
            throw new Exception("Failed to generate SQL with few-shot learning.");
        }

        private (string Sql, string Explanation) ParseSqlAndExplanation(string content)
        {
            string sql = string.Empty;
            string explanation = string.Empty;

            var sqlStart = content.IndexOf("---SQL_START---");
            var sqlEnd = content.IndexOf("---SQL_END---");
            var expStart = content.IndexOf("---EXPLANATION_START---");
            var expEnd = content.IndexOf("---EXPLANATION_END---");

            if (sqlStart >= 0 && sqlEnd >= 0 && sqlEnd > sqlStart)
            {
                sql = content.Substring(sqlStart + 15, sqlEnd - sqlStart - 15).Trim();
            }

            if (expStart >= 0 && expEnd >= 0 && expEnd > expStart)
            {
                explanation = content.Substring(expStart + 21, expEnd - expStart - 21).Trim();
            }

            // Fallback: if parsing fails, treat entire content as SQL
            if (string.IsNullOrEmpty(sql))
            {
                sql = content.Replace("```sql", "").Replace("```", "").Trim();
                explanation = "Auto-generated SQL query based on your request.";
            }

            return (sql, explanation);
        }
    }
}