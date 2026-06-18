using AiSqlAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiSqlAssistant.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Automatically creates the AuditLogs table in SQLite
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<QueryHistory> QueryHistory { get; set; }
        public DbSet<QueryTemplate> QueryTemplates { get; set; }
    }
}