using Microsoft.EntityFrameworkCore;
using VpsMonitor.Web.Data.Entities;
using VpsMonitor.Web.Security;

namespace VpsMonitor.Web.Data;

public sealed class MonitorDbContext(DbContextOptions<MonitorDbContext> options) : DbContext(options), IMonitorStore
{
    public DbSet<MonitorUser> MonitorUsers => Set<MonitorUser>();
    public DbSet<MonitorSession> MonitorSessions => Set<MonitorSession>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<HealthCheckDefinition> HealthCheckDefinitions => Set<HealthCheckDefinition>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public Task<MonitorUser?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return MonitorUsers.SingleOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    public async Task AddSessionAsync(MonitorSession session, CancellationToken cancellationToken = default)
    {
        MonitorSessions.Add(session);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<MonitorSession?> FindSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return MonitorSessions.Include(session => session.MonitorUser)
            .SingleOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAuditAsync(AuditEntry audit, CancellationToken cancellationToken = default)
    {
        AuditEntries.Add(audit);
        await SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonitorUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(user => user.Username).IsUnique();
        });

        modelBuilder.Entity<MonitorSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(session => session.TokenHash).IsUnique();
            entity.HasOne(session => session.MonitorUser)
                .WithMany(user => user.Sessions)
                .HasForeignKey(session => session.MonitorUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.ProjectKey).HasMaxLength(120).IsRequired();
            entity.HasIndex(assignment => assignment.ProjectKey);
            entity.HasOne(assignment => assignment.MonitorUser)
                .WithMany(user => user.ProjectAssignments)
                .HasForeignKey(assignment => assignment.MonitorUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HealthCheckDefinition>(entity =>
        {
            entity.HasKey(check => check.Id);
            entity.Property(check => check.ProjectKey).HasMaxLength(120).IsRequired();
            entity.Property(check => check.Name).HasMaxLength(120).IsRequired();
            entity.Property(check => check.Url).HasMaxLength(2048).IsRequired();
            entity.Property(check => check.Method).HasMaxLength(16).IsRequired();
            entity.HasIndex(check => check.ProjectKey);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Username).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.Action).HasMaxLength(80).IsRequired();
            entity.Property(audit => audit.Target).HasMaxLength(160).IsRequired();
            entity.Property(audit => audit.RequestIp).HasMaxLength(64).IsRequired();
            entity.Property(audit => audit.UserAgent).HasMaxLength(512).IsRequired();
            entity.Property(audit => audit.Detail).HasMaxLength(2048).IsRequired();
            entity.HasIndex(audit => audit.OccurredAtUtc);
        });
    }
}
