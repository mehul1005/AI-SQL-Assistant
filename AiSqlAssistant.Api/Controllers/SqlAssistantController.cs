using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions; // 1. Added Regex for security scanning

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

            // --- DYNAMIC SCHEMA DISCOVERY ---
            var schemaBuilder = new System.Text.StringBuilder();
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                using var schemaReader = await schemaCommand.ExecuteReaderAsync();
                while (await schemaReader.ReadAsync())
                {
                    if (!schemaReader.IsDBNull(0))
                    {
                        schemaBuilder.AppendLine(schemaReader.GetString(0));
                        schemaBuilder.AppendLine(";");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to discover database schema: {ex.Message}");
            }

            string discoveredSchema = schemaBuilder.ToString();
            // --- END SCHEMA DISCOVERY ---

            // 1. Generate the SQL string
            string sqlQuery = await _sqlGeneratorService.GenerateSqlAsync(request.UserPrompt, discoveredSchema);
            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // --- NEW: PHASE 4 SECURITY LAYER ---
            // We use \b (word boundaries) so we don't accidentally block a column named "DropoffTime"
            string forbiddenPattern = @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|CREATE|EXEC|EXECUTE|GRANT|REVOKE)\b";

            if (Regex.IsMatch(sqlQuery, forbiddenPattern, RegexOptions.IgnoreCase))
            {
                // Abort execution and return a security warning to the client
                return Ok(new
                {
                    GeneratedSql = sqlQuery,
                    Error = "SECURITY ALERT: Destructive DML/DDL query detected and blocked. This agent is restricted to read-only SELECT statements.",
                    Data = Array.Empty<object>()
                });
            }
            // --- END SECURITY LAYER ---

            // 2. Execute the safe SQL against SQLite
            var queryRows = new List<Dictionary<string, object>>();

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
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