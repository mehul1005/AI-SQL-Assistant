using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
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

            string sqlQuery = await _sqlGeneratorService.GenerateSqlAsync(request.UserPrompt, schemaBuilder.ToString());
            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // Generate Risk Profile
            var riskProfile = QueryRiskAnalyzer.Analyze(sqlQuery);

            // --- LOG CRITICAL ATTEMPTS IMMEDIATELY ---
            if (riskProfile.IsExecutionBlocked)
            {
                var auditRecord = new AuditLog
                {
                    UserPrompt = request.UserPrompt,
                    GeneratedSql = sqlQuery,
                    RiskLevel = riskProfile.RiskLevel,
                    Status = "BLOCKED_BY_ANALYZER", // Distinct status for generation-blocks
                    ExecutionDurationMs = 0, // 0 because it never reached the DB execution
                    ErrorMessage = "Query execution was locked by the Risk Analyzer."
                };

                _dbContext.AuditLogs.Add(auditRecord);
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { GeneratedSql = sqlQuery, RiskProfile = riskProfile });
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteSql([FromBody] SqlExecutionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SqlQuery))
                return BadRequest("SQL query cannot be empty.");

            string sqlQuery = request.SqlQuery.Trim();
            var stopwatch = Stopwatch.StartNew();

            // Create Audit Record
            var auditRecord = new AuditLog
            {
                UserPrompt = request.OriginalPrompt ?? "Unknown",
                GeneratedSql = sqlQuery,
                RiskLevel = request.RiskLevel ?? "UNKNOWN"
            };

            // Security Interceptor
            string forbiddenPattern = @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|CREATE|EXEC|EXECUTE|GRANT|REVOKE)\b";
            if (Regex.IsMatch(sqlQuery, forbiddenPattern, RegexOptions.IgnoreCase))
            {
                stopwatch.Stop();
                auditRecord.Status = "BLOCKED_SECURITY";
                auditRecord.ErrorMessage = "Destructive DML/DDL query detected.";
                auditRecord.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

                _dbContext.AuditLogs.Add(auditRecord);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    GeneratedSql = sqlQuery,
                    Error = "SECURITY ALERT: Destructive DML/DDL query detected and blocked.",
                    Data = Array.Empty<object>()
                });
            }

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

                stopwatch.Stop();
                auditRecord.Status = "EXECUTED";
                auditRecord.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

                _dbContext.AuditLogs.Add(auditRecord);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                auditRecord.Status = "ERROR";
                auditRecord.ErrorMessage = ex.Message;
                auditRecord.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

                _dbContext.AuditLogs.Add(auditRecord);
                await _dbContext.SaveChangesAsync();

                return Ok(new { GeneratedSql = sqlQuery, Error = $"Database execution error: {ex.Message}", Data = Array.Empty<object>() });
            }

            return Ok(new { GeneratedSql = sqlQuery, Error = string.Empty, Data = queryRows });
        }

        // Quick endpoint to view logs!
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _dbContext.AuditLogs.OrderByDescending(a => a.Timestamp).Take(50).ToListAsync();
            return Ok(logs);
        }
    }
}