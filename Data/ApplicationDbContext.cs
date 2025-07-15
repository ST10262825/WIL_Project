using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<TutorModule> TutorModules { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Availability> Availabilities { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TutorModule>()
                .HasKey(tm => new { tm.TutorId, tm.ModuleId });

            // 🔧 Prevent multiple cascade paths for Session
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Tutor)
                .WithMany()
                .HasForeignKey(s => s.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Module)
                .WithMany()
                .HasForeignKey(s => s.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rating>()
    .HasOne(r => r.Tutor)
    .WithMany(t => t.Ratings)
    .HasForeignKey(r => r.TutorId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TutorModule>()
    .HasKey(tm => new { tm.TutorId, tm.ModuleId });

            modelBuilder.Entity<TutorModule>()
                .HasOne(tm => tm.Tutor)
                .WithMany(t => t.TutorModules)
                .HasForeignKey(tm => tm.TutorId);

            
            // or .NoAction

        }

    }
}
