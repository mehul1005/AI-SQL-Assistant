using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

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

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSql([FromBody] SqlGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
                return BadRequest("Prompt cannot be empty.");

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

            // 1. Generate the SQL string
            string sqlQuery = await _sqlGeneratorService.GenerateSqlAsync(request.UserPrompt, schemaBuilder.ToString());
            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // Return ONLY the generated SQL (No execution yet)
            return Ok(new { GeneratedSql = sqlQuery });
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteSql([FromBody] SqlExecutionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SqlQuery))
                return BadRequest("SQL query cannot be empty.");

            string sqlQuery = request.SqlQuery.Trim();

            // --- PHASE 4 SECURITY LAYER (Protects against AI AND Human edits) ---
            string forbiddenPattern = @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|CREATE|EXEC|EXECUTE|GRANT|REVOKE)\b";
            if (System.Text.RegularExpressions.Regex.IsMatch(sqlQuery, forbiddenPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return Ok(new
                {
                    GeneratedSql = sqlQuery,
                    Error = "SECURITY ALERT: Destructive DML/DDL query detected and blocked. This agent is restricted to read-only SELECT statements.",
                    Data = Array.Empty<object>()
                });
            }

            // 2. Execute the safe SQL against SQLite
            var queryRows = new List<Dictionary<string, object>>();
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
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