using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public sealed class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options)
    : DbContext(options)
{
    public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable("AdmissionClasses", table =>
                table.HasCheckConstraint(
                    "CK_AdmissionClasses_TotalSeats_Positive",
                    "\"TotalSeats\" > 0"));

            entity.HasKey(admissionClass => admissionClass.Id);
            entity.Property(admissionClass => admissionClass.Id).ValueGeneratedNever();
            entity.Property(admissionClass => admissionClass.Name).IsRequired();
            entity.Property(admissionClass => admissionClass.TotalSeats).IsRequired();

            entity.HasMany(admissionClass => admissionClass.Holds)
                .WithOne(hold => hold.AdmissionClass)
                .HasForeignKey(hold => hold.AdmissionClassId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            entity.ToTable("AdmissionHolds", table =>
            {
                table.HasCheckConstraint(
                    "CK_AdmissionHolds_Status",
                    "\"Status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                table.HasCheckConstraint(
                    "CK_AdmissionHolds_ExpirationAfterCreation",
                    "\"ExpiresAt\" > \"CreatedAt\"");
            });

            entity.HasKey(hold => hold.Id);
            entity.Property(hold => hold.Id).ValueGeneratedNever();
            entity.Property(hold => hold.AdmissionClassId).IsRequired();
            entity.Property(hold => hold.StudentRegistrationId).IsRequired();
            entity.Property(hold => hold.Status)
                .HasConversion<string>()
                .IsRequired();
            entity.Property(hold => hold.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(hold => hold.ExpiresAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(hold => new { hold.AdmissionClassId, hold.StudentRegistrationId });
            entity.HasIndex(hold => new { hold.AdmissionClassId, hold.Status });
        });
    }
}
