using Microsoft.EntityFrameworkCore;

namespace Server
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.FirstName)
                .HasColumnType("nvarchar(max)");

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UCode> UCodes { get; set; }
    }
}