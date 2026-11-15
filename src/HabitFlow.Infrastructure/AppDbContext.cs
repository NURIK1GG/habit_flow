using HabitFlow.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Infrastructure;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Routine> Routines => Set<Routine>();
    public DbSet<RoutineStep> RoutineSteps => Set<RoutineStep>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().Property(u => u.WeeklyGoal).HasDefaultValue(5);
        b.Entity<AppUser>().ToTable(t => t.HasCheckConstraint("CK_User_WeeklyGoal", "WeeklyGoal BETWEEN 1 AND 100"));
        b.Entity<AppUser>().Property(u => u.DisplayName).HasMaxLength(40);
        b.Entity<AppUser>().Property(u => u.LockoutEnd).HasConversion(
            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero)));
        b.Entity<Routine>(e =>
        {
            e.Property(r => r.Title).HasMaxLength(80).IsRequired();
            e.Property(r => r.Description).HasMaxLength(500);
            e.Property(r => r.Revision).IsConcurrencyToken();
            e.HasIndex(r => new { r.OwnerId, r.Archived });
            e.HasOne<AppUser>().WithMany().HasForeignKey(r => r.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(r => r.Steps).WithOne().HasForeignKey(s => s.RoutineId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(r => r.CheckIns).WithOne().HasForeignKey(c => c.RoutineId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Routine_Minutes", "TargetMinutes BETWEEN 1 AND 600");
                t.HasCheckConstraint("CK_Routine_Days", "DaysOfWeek BETWEEN 1 AND 127");
            });
        });
        b.Entity<RoutineStep>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(80).IsRequired();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Step_Sets", "Sets BETWEEN 1 AND 30");
                t.HasCheckConstraint("CK_Step_Reps", "Repetitions BETWEEN 1 AND 500");
            });
        });
        b.Entity<CheckIn>(e =>
        {
            e.HasIndex(c => new { c.RoutineId, c.Date }).IsUnique(); e.HasIndex(c => c.Date);
            e.Property(c => c.Notes).HasMaxLength(300);
            e.ToTable(t => t.HasCheckConstraint("CK_CheckIn_Minutes", "Minutes BETWEEN 1 AND 600"));
        });
    }
}
