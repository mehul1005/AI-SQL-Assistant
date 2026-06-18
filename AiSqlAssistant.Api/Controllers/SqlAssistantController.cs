using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AiSqlAssistant.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SqlAssistantController : ControllerBase
    {
        private readonly ISqlGeneratorService _sqlGeneratorService;
        private readonly ApplicationDbContext _dbContext;
        private readonly TelemetryClient _telemetry;

        public SqlAssistantController(ISqlGeneratorService sqlGeneratorService, ApplicationDbContext dbContext, TelemetryClient telemetry)
        {
            _sqlGeneratorService = sqlGeneratorService;
            _dbContext = dbContext;
            _telemetry = telemetry;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSql([FromBody] SqlGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
                return BadRequest("Prompt cannot be empty.");

            var schemaBuilder = new System.Text.StringBuilder();
            try
            {
                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var schemaCommand = connection.CreateCommand())
                {
                    // Interrogate Azure SQL for all tables and their exact column definitions
                    schemaCommand.CommandText = @"
                        SELECT 
                            t.TABLE_NAME,
                            STUFF((
                                SELECT ', ' + c.COLUMN_NAME + ' ' + c.DATA_TYPE
                                FROM INFORMATION_SCHEMA.COLUMNS c
                                WHERE c.TABLE_NAME = t.TABLE_NAME
                                FOR XML PATH('')
                            ), 1, 2, '') AS Columns
                        FROM INFORMATION_SCHEMA.TABLES t
                        WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_NAME != '__EFMigrationsHistory'";

                    using (var schemaReader = await schemaCommand.ExecuteReaderAsync())
                    {
                        while (await schemaReader.ReadAsync())
                        {
                            string tableName = schemaReader.GetString(0);
                            string columns = schemaReader.GetString(1);

                            // Feeds the LLM a clean format: "Table: Applications | Columns: Id int, AppName nvarchar..."
                            schemaBuilder.AppendLine($"Table: {tableName} | Columns: {columns}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to discover database schema: {ex.Message}");
            }

            string sqlQuery;
            string? explanation = null;

            // Use few-shot learning if examples are provided
            if (request.FewShotExamples != null && request.FewShotExamples.Any())
            {
                sqlQuery = await _sqlGeneratorService.GenerateSqlWithFewShotAsync(
                    request.UserPrompt, 
                    schemaBuilder.ToString(), 
                    request.FewShotExamples);
            }
            // Use explanation mode if requested
            else if (request.IncludeExplanation)
            {
                var (sql, exp) = await _sqlGeneratorService.GenerateSqlWithExplanationAsync(
                    request.UserPrompt, 
                    schemaBuilder.ToString());
                sqlQuery = sql;
                explanation = exp;
            }
            // Standard generation
            else
            {
                sqlQuery = await _sqlGeneratorService.GenerateSqlAsync(request.UserPrompt, schemaBuilder.ToString());
            }

            sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

            // Generate Risk Profile
            var riskProfile = QueryRiskAnalyzer.Analyze(sqlQuery);

            // Save to Query History (Phase 1 feature)
            var historyRecord = new QueryHistory
            {
                UserPrompt = request.UserPrompt,
                GeneratedSql = sqlQuery,
                UserName = HttpContext.User.Identity?.Name ?? "Unknown",
                WasExecuted = false,
                Explanation = explanation
            };
            _dbContext.QueryHistory.Add(historyRecord);
            await _dbContext.SaveChangesAsync();

            // --- LOG CRITICAL ATTEMPTS IMMEDIATELY ---
            if (riskProfile.IsExecutionBlocked)
            {
                var auditRecord = new AuditLog
                {
                    UserPrompt = request.UserPrompt,
                    GeneratedSql = sqlQuery,
                    RiskLevel = riskProfile.RiskLevel,
                    Status = "BLOCKED_BY_ANALYZER",
                    ExecutionDurationMs = 0,
                    ErrorMessage = "Query execution was locked by the Risk Analyzer.",
                    UserName = HttpContext.User.Identity?.Name ?? "Unknown"
                };

                _dbContext.AuditLogs.Add(auditRecord);
                await _dbContext.SaveChangesAsync();
            }

            // --- CUSTOM TELEMETRY ---
            _telemetry.TrackEvent("SqlGenerated", new Dictionary<string, string>
            {
                { "User", HttpContext.User.Identity?.Name ?? "Unknown" },
                { "RiskLevel", riskProfile.RiskLevel },
                { "BlockedByAnalyzer", riskProfile.IsExecutionBlocked.ToString() },
                { "HasExplanation", (!string.IsNullOrEmpty(explanation)).ToString() }
            });

            return Ok(new { GeneratedSql = sqlQuery, RiskProfile = riskProfile, Explanation = explanation });
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
                RiskLevel = request.RiskLevel ?? "UNKNOWN",
                UserName = HttpContext.User.Identity?.Name ?? "Unknown"
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

                // --- CUSTOM TELEMETRY ---
                _telemetry.TrackEvent("SecurityInterceptorTriggered", new Dictionary<string, string>
                {
                    { "User", HttpContext.User.Identity?.Name ?? "Unknown" },
                    { "AttemptedQuery", sqlQuery }
                });

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

        [HttpGet("history")]
        public async Task<IActionResult> GetQueryHistory()
        {
            var history = await _dbContext.QueryHistory
                .OrderByDescending(h => h.Timestamp)
                .Take(100)
                .ToListAsync();
            return Ok(history);
        }

        [HttpPost("templates")]
        public async Task<IActionResult> SaveTemplate([FromBody] QueryTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Name) || string.IsNullOrWhiteSpace(template.SqlTemplate))
                return BadRequest("Name and SQL template are required.");

            template.CreatedBy = HttpContext.User.Identity?.Name ?? "Unknown";
            template.CreatedDate = DateTime.UtcNow;
            
            _dbContext.QueryTemplates.Add(template);
            await _dbContext.SaveChangesAsync();
            
            return Ok(template);
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates(string? category = null)
        {
            IQueryable<QueryTemplate> query = _dbContext.QueryTemplates;
            
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }
            
            var templates = await query.OrderByDescending(t => t.UsageCount).ToListAsync();
            return Ok(templates);
        }

        [HttpPut("templates/{id}/use")]
        public async Task<IActionResult> IncrementTemplateUsage(int id)
        {
            var template = await _dbContext.QueryTemplates.FindAsync(id);
            if (template == null)
                return NotFound();
            
            template.UsageCount++;
            await _dbContext.SaveChangesAsync();
            
            return Ok(template);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            // Fetch all logs once so we can manipulate them safely in memory (for 11 rows, this is instant)
            var allLogs = await _dbContext.AuditLogs.ToListAsync();

            var total = allLogs.Count;

            // Check for "BLOCKED" regardless of case
            var blocked = allLogs.Count(l =>
                l.Status != null && l.Status.ToLower().Contains("blocked"));

            // Check for "CRITICAL" regardless of case
            var critical = allLogs.Count(l =>
                l.RiskLevel != null && l.RiskLevel.ToLower() == "critical");

            // Calculate average safely
            var avgDur = allLogs
                .Where(l => l.ExecutionDurationMs > 0)
                .Select(l => (double)l.ExecutionDurationMs)
                .DefaultIfEmpty(0)
                .Average();

            // Grouping users
            var topUsers = allLogs
                .GroupBy(l => l.UserName ?? "Unknown")
                .Select(g => new UserActivity
                {
                    UserName = g.Key,
                    QueryCount = g.Count()
                })
                .OrderByDescending(x => x.QueryCount)
                .Take(5)
                .ToList();

            var summary = new AnalyticsSummary
            {
                TotalQueries = total,
                BlockedQueries = blocked,
                CriticalRisks = critical,
                AverageDurationMs = Math.Round(avgDur, 2),
                TopUsers = topUsers
            };

            return Ok(summary);
        }
    }

    public class AnalyticsSummary
    {
        public int TotalQueries { get; set; }
        public int BlockedQueries { get; set; }
        public int CriticalRisks { get; set; }
        public double AverageDurationMs { get; set; }
        public List<UserActivity> TopUsers { get; set; } = new();
    }

    public class UserActivity
    {
        public string UserName { get; set; } = string.Empty;
        public int QueryCount { get; set; }
    }
}