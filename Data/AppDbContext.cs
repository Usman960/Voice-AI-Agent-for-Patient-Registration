using Microsoft.EntityFrameworkCore;
using Voice_AI_Agent.Model;

namespace Voice_AI_Agent.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("patients");

                entity.HasKey(p => p.PatientId);
                entity.Property(p => p.PatientId)
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.FirstName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(p => p.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(p => p.DateOfBirth)
                    .IsRequired();

                entity.Property(p => p.Sex)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(p => p.PhoneNumber)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(p => p.Email)
                    .HasMaxLength(254);

                entity.Property(p => p.AddressLine1)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.AddressLine2)
                    .HasMaxLength(100);

                entity.Property(p => p.City)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(p => p.State)
                    .HasMaxLength(2)
                    .IsRequired();

                entity.Property(p => p.ZipCode)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(p => p.InsuranceProvider)
                    .HasMaxLength(100);

                entity.Property(p => p.InsuranceMemberId)
                    .HasMaxLength(50);

                entity.Property(p => p.PreferredLanguage)
                    .HasMaxLength(50)
                    .HasDefaultValue("English");

                entity.Property(p => p.EmergencyContactName)
                    .HasMaxLength(200);

                entity.Property(p => p.EmergencyContactPhone)
                    .HasMaxLength(10);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.UpdatedAt)
                    .IsRequired();

                entity.Property(p => p.DeletedAt);
            });
        }
    }
}
