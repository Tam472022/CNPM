using Microsoft.EntityFrameworkCore;
using Duan_CNPM.Models;

namespace Duan_CNPM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Council> Councils { get; set; }
        public DbSet<CouncilMember> CouncilMembers { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<ProjectProgress> ProjectProgresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<ProfessorComment> ProfessorComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Project relationships
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Student)
                .WithMany(u => u.ProjectsAsStudent)
                .HasForeignKey(p => p.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Professor)
                .WithMany(u => u.ProjectsAsProfessor)
                .HasForeignKey(p => p.ProfessorID)
                .OnDelete(DeleteBehavior.Restrict);

            // Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverID)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Administrator",
                    Role = "Admin",
                    Email = "admin@cnpm.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed system configs
            modelBuilder.Entity<SystemConfig>().HasData(
                new SystemConfig
                {
                    ConfigID = 1,
                    ConfigKey = "CurrentYear",
                    ConfigValue = DateTime.Now.Year.ToString(),
                    Description = "Năm học hiện tại"
                },
                new SystemConfig
                {
                    ConfigID = 2,
                    ConfigKey = "CurrentSemester",
                    ConfigValue = "1",
                    Description = "Học kỳ hiện tại"
                },
                new SystemConfig
                {
                    ConfigID = 3,
                    ConfigKey = "MaxFileSize",
                    ConfigValue = "10485760",
                    Description = "Kích thước file tối đa (bytes) - 10MB"
                }
            );
        }
    }
}