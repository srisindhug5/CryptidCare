using CryptidCare.Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Claims.Data.Persistence;

/// <summary>
/// Entity Framework Core database context for claims, patients, medicines, and rule audits.
/// </summary>
public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    /// <summary>Patients table.</summary>
    public DbSet<Patient> Patients => Set<Patient>();

    /// <summary>Medicines table.</summary>
    public DbSet<Medicine> Medicines => Set<Medicine>();

    /// <summary>Claims table.</summary>
    public DbSet<Claim> Claims => Set<Claim>();

    /// <summary>Per-rule evaluation audit rows.</summary>
    public DbSet<ClaimRuleEvaluation> ClaimRuleEvaluations => Set<ClaimRuleEvaluation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TrueName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.HeadCount).HasDefaultValue(1);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.ToTable("Medicines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BaseCost).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("Claims");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.RejectionReason).HasMaxLength(500);
            entity.Property(x => x.RejectionCode).HasConversion<int>();
            entity.Property(x => x.CreatedAtUtc)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();
            entity.HasIndex(x => new { x.PatientId, x.MedicineId, x.CreatedAtUtc })
                .HasDatabaseName("IX_Claims_PatientMedicineDuplicateCheck")
                .IsDescending(false, false, true);
            entity.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Medicine).WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.RuleEvaluations).WithOne(x => x.Claim).HasForeignKey(x => x.ClaimId);
        });

        modelBuilder.Entity<ClaimRuleEvaluation>(entity =>
        {
            entity.ToTable("ClaimRuleEvaluations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RuleName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.RejectionCode).HasConversion<int?>();
            entity.Property(x => x.EvaluatedAtUtc)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();
        });
    }
}
