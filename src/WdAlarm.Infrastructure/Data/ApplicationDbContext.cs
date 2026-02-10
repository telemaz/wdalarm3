using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data;

/// <summary>
/// Application database context with ASP.NET Core Identity support
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Alarm> Alarms => Set<Alarm>();
    public DbSet<AlarmCycle> AlarmCycles => Set<AlarmCycle>();
    public DbSet<AlarmDelayAction> AlarmDelayActions => Set<AlarmDelayAction>();
    public DbSet<AlarmPingAction> AlarmPingActions => Set<AlarmPingAction>();
    public DbSet<Ping> Pings => Set<Ping>();
    public DbSet<ActionExecution> ActionExecutions => Set<ActionExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Configure Identity tables to use Guid as primary key
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
        });
        
        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });
        
        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserRoles");
        });
        
        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserClaims");
        });
        
        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserLogins");
        });
        
        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserTokens");
        });
        
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims");
        });
    }
}
