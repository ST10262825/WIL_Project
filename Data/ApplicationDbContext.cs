using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<TutorModule> TutorModules { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Booking> Bookings { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite key for TutorModule
            modelBuilder.Entity<TutorModule>()
                .HasKey(tm => new { tm.TutorId, tm.ModuleId });

            // Relationships for TutorModule
            modelBuilder.Entity<TutorModule>()
                .HasOne(tm => tm.Tutor)
                .WithMany(t => t.TutorModules)
                .HasForeignKey(tm => tm.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TutorModule>()
                .HasOne(tm => tm.Module)
                .WithMany(m => m.TutorModules)
                .HasForeignKey(tm => tm.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session relationships
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Tutor)
                .WithMany(t => t.Sessions)
                .HasForeignKey(s => s.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Sessions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Module)
                .WithMany(m => m.Sessions)
                .HasForeignKey(s => s.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<ChatMessage>()
         .HasOne(c => c.Sender)
         .WithMany()
         .HasForeignKey(c => c.SenderId)
         .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);



            //// Rating relationships
            //modelBuilder.Entity<Rating>()
            //    .HasOne(r => r.Tutor)
            //    .WithMany(t => t.Ratings)
            //    .HasForeignKey(r => r.TutorId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //// Availability relationships
            //modelBuilder.Entity<Availability>()
            //    .HasOne(a => a.Tutor)
            //    .WithMany(t => t.Availabilities)
            //    .HasForeignKey(a => a.TutorId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //// Message relationships
            //modelBuilder.Entity<Message>()
            //    .HasOne(m => m.Sender)
            //    .WithMany(u => u.SentMessages)
            //    .HasForeignKey(m => m.SenderId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<Message>()
            //    .HasOne(m => m.Receiver)
            //    .WithMany(u => u.ReceivedMessages)
            //    .HasForeignKey(m => m.ReceiverId)
            //    .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
