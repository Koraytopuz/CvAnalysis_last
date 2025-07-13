using CvAnalysis.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CvAnalysis.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
    }
}