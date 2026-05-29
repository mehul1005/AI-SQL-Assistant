using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AiSqlAssistant.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SqlAssistantController : ControllerBase
    {
        private readonly ISqlGeneratorService _sqlGeneratorService;
        private readonly ApplicationDbContext _dbContext;

        public SqlAssistantController(ISqlGeneratorService sqlGeneratorService, ApplicationDbContext dbContext)
        {
            _sqlGeneratorService = sqlGeneratorService;
            _dbContext = dbContext;
        }

        [HttpPost("generate-sql")]
        // Changed parameter to use your existing SqlGenerationRequest model
        public async Task<IActionResult> GenerateAndExecuteSql([FromBody] SqlGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            // 1. Generate the SQL string via Groq/Llama
            // Pass the single request object, and extract the string from the response
            var aiResponse = await _sqlGeneratorService.GenerateSqlAsync(request);
            string sqlQuery = aiResponse.GeneratedSql;

            // Clean up any markdown code blocks the LLM might have wrapped the query in
            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // 2. Dynamically execute the raw SQL against our SQLite database
            var queryRows = new List<Dictionary<string, object>>();

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    queryRows.Add(row);
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    GeneratedSql = sqlQuery,
                    Error = $"Database execution error: {ex.Message}",
                    Data = Array.Empty<object>()
                });
            }

            // Return both the generated query text and the real dataset back to the client
            return Ok(new
            {
                GeneratedSql = sqlQuery,
                Error = string.Empty,
                Data = queryRows
            });
        }
    }
}