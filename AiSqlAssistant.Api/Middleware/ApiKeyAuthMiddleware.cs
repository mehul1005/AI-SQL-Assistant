using AiSqlAssistant.Api.Data;
using AiSqlAssistant.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AiSqlAssistant.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEYNAME = "x-api-key";

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Inject ApplicationDbContext directly here so the Middleware can talk to SQLite!
        public async Task InvokeAsync(HttpContext context, IConfiguration config, ApplicationDbContext dbContext)
        {
            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                await LogUnauthorizedAttempt(dbContext, "Missing API Key");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }

            var apiKeys = config.GetSection("ApiKeys").Get<Dictionary<string, string>>();

            if (apiKeys == null || !apiKeys.TryGetValue(extractedApiKey, out var userName))
            {
                await LogUnauthorizedAttempt(dbContext, $"Invalid API Key Used: {extractedApiKey}");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Unauthorized client. Invalid API Key.");
                return;
            }

            var claims = new[] { new Claim(ClaimTypes.Name, userName) };
            var identity = new ClaimsIdentity(claims, "ApiKey");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        // Private helper method to write the hacker attempt to the database
        private async Task LogUnauthorizedAttempt(ApplicationDbContext dbContext, string reason)
        {
            var auditRecord = new AuditLog
            {
                UserPrompt = "N/A",
                GeneratedSql = "N/A",
                RiskLevel = "CRITICAL",
                Status = "BLOCKED_AUTH",
                ErrorMessage = $"Authentication Failed: {reason}",
                UserName = "Unknown/Attacker",
                ExecutionDurationMs = 0
            };

            dbContext.AuditLogs.Add(auditRecord);
            await dbContext.SaveChangesAsync();
        }
    }
}