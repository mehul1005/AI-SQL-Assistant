using Microsoft.EntityFrameworkCore;

namespace AiSqlAssistant.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // No DbSets needed here! We will use raw ADO.NET underneath EF Core for dynamic execution.
    }
}