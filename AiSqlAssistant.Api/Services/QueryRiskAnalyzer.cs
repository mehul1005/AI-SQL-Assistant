using AiSqlAssistant.Api.Models;
using System.Text.RegularExpressions;

namespace AiSqlAssistant.Api.Services
{
    public static class QueryRiskAnalyzer
    {
        public static QueryRiskProfile Analyze(string sqlQuery)
        {
            var profile = new QueryRiskProfile();
            string upperSql = sqlQuery.ToUpper();

            // 1. Determine Operation Type
            if (upperSql.Contains("SELECT")) profile.Operation = "SELECT";
            if (upperSql.Contains("INSERT")) profile.Operation = "INSERT";
            if (upperSql.Contains("UPDATE")) profile.Operation = "UPDATE";
            if (upperSql.Contains("DELETE")) profile.Operation = "DELETE";
            if (upperSql.Contains("DROP")) profile.Operation = "DROP";
            if (upperSql.Contains("TRUNCATE")) profile.Operation = "TRUNCATE";

            // 2. Extract Table Names (Naive approach for SQLite/SQL Server)
            var fromMatches = Regex.Matches(upperSql, @"(?:FROM|UPDATE|INTO|JOIN)\s+([A-Z0-9_]+)");
            foreach (Match match in fromMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string table = match.Groups[1].Value;
                    if (!profile.AffectedTables.Contains(table) && table != "SELECT")
                    {
                        profile.AffectedTables.Add(table);
                    }
                }
            }

            // 3. Calculate Risk Level
            string forbiddenPattern = @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|CREATE|EXEC|GRANT)\b";

            if (Regex.IsMatch(upperSql, forbiddenPattern))
            {
                profile.RiskLevel = "CRITICAL";
                profile.IsExecutionBlocked = true;
            }
            else if (!upperSql.Contains("WHERE") && upperSql.Contains("SELECT"))
            {
                // A SELECT without a WHERE clause could return millions of rows, slowing down the server
                profile.RiskLevel = "MEDIUM";
            }
            else
            {
                profile.RiskLevel = "LOW";
            }

            return profile;
        }
    }
}