using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Role> Role { get; set; }
    public DbSet<Account> Account { get; set; }
    public DbSet<Employee> Employee { get; set; }
    public DbSet<Device> Device { get; set; }
    public DbSet<DeviceEmployee> DeviceEmployee { get; set; }
    public DbSet<Position> Position { get; set; }
    public DbSet<DeviceType> DeviceType { get; set; }
    public DbSet<Person> Person { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Username)
            .IsUnique();
    }
}