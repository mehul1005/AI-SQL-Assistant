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
        public async Task<IActionResult> GenerateAndExecuteSql([FromBody] SqlGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            // --- NEW: DYNAMIC SCHEMA DISCOVERY ---
            var schemaBuilder = new System.Text.StringBuilder();
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // Query SQLite's internal master table for the exact CREATE TABLE statements
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                using var schemaReader = await schemaCommand.ExecuteReaderAsync();
                while (await schemaReader.ReadAsync())
                {
                    if (!schemaReader.IsDBNull(0))
                    {
                        schemaBuilder.AppendLine(schemaReader.GetString(0));
                        schemaBuilder.AppendLine(";"); // Add a semicolon for clean formatting
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to discover database schema: {ex.Message}");
            }

            string discoveredSchema = schemaBuilder.ToString();

            // --- END DYNAMIC SCHEMA DISCOVERY ---

            // 1. Generate the SQL string via Groq/Llama (We pass the discovered schema manually now)
            // NOTE: You may need to update your ISqlGeneratorService interface to accept (UserPrompt, DiscoveredSchema)
            string sqlQuery = await _sqlGeneratorService.GenerateSqlAsync(request.UserPrompt, discoveredSchema);

            // Clean up any markdown code blocks the LLM might have wrapped the query in
            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // 2. Dynamically execute the raw SQL against our SQLite database
            var queryRows = new List<Dictionary<string, object>>();

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                // Check if connection is already open from our schema query
                if (connection.State != ConnectionState.Open)
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
                return Ok(new { GeneratedSql = sqlQuery, Error = $"Database execution error: {ex.Message}", Data = Array.Empty<object>() });
            }

            return Ok(new { GeneratedSql = sqlQuery, Error = string.Empty, Data = queryRows });
        }
    }
}