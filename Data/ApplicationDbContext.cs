//using Microsoft.EntityFrameworkCore;
//using TutorConnectAPI.Models;

//namespace TutorConnectAPI.Data
//{
//    public class ApplicationDbContext : DbContext
//    {
//        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

//        // DbSets
//        public DbSet<User> Users { get; set; }
//        public DbSet<Student> Students { get; set; }
//        public DbSet<Tutor> Tutors { get; set; }
//        public DbSet<Module> Modules { get; set; }
//        public DbSet<TutorModule> TutorModules { get; set; }
//        public DbSet<Session> Sessions { get; set; }
//        public DbSet<ChatMessage> ChatMessages { get; set; }

//        public DbSet<Booking> Bookings { get; set; }




//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            // Composite key for TutorModule
//            modelBuilder.Entity<TutorModule>()
//                .HasKey(tm => new { tm.TutorId, tm.ModuleId });

//            // Relationships for TutorModule
//            modelBuilder.Entity<TutorModule>()
//                .HasOne(tm => tm.Tutor)
//                .WithMany(t => t.TutorModules)
//                .HasForeignKey(tm => tm.TutorId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<TutorModule>()
//                .HasOne(tm => tm.Module)
//                .WithMany(m => m.TutorModules)
//                .HasForeignKey(tm => tm.ModuleId)
//                .OnDelete(DeleteBehavior.Cascade);

//            // Session relationships
//            modelBuilder.Entity<Session>()
//                .HasOne(s => s.Tutor)
//                .WithMany(t => t.Sessions)
//                .HasForeignKey(s => s.TutorId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<Session>()
//                .HasOne(s => s.Student)
//                .WithMany(st => st.Sessions)
//                .HasForeignKey(s => s.StudentId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<Session>()
//                .HasOne(s => s.Module)
//                .WithMany(m => m.Sessions)
//                .HasForeignKey(s => s.ModuleId)
//                .OnDelete(DeleteBehavior.Restrict);



//            modelBuilder.Entity<ChatMessage>()
//         .HasOne(c => c.Sender)
//         .WithMany()
//         .HasForeignKey(c => c.SenderId)
//         .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<ChatMessage>()
//                .HasOne(c => c.Receiver)
//                .WithMany()
//                .HasForeignKey(c => c.ReceiverId)
//                .OnDelete(DeleteBehavior.Restrict);



//            //// Rating relationships
//            //modelBuilder.Entity<Rating>()
//            //    .HasOne(r => r.Tutor)
//            //    .WithMany(t => t.Ratings)
//            //    .HasForeignKey(r => r.TutorId)
//            //    .OnDelete(DeleteBehavior.Restrict);

//            //// Availability relationships
//            //modelBuilder.Entity<Availability>()
//            //    .HasOne(a => a.Tutor)
//            //    .WithMany(t => t.Availabilities)
//            //    .HasForeignKey(a => a.TutorId)
//            //    .OnDelete(DeleteBehavior.Cascade);

//            //// Message relationships
//            //modelBuilder.Entity<Message>()
//            //    .HasOne(m => m.Sender)
//            //    .WithMany(u => u.SentMessages)
//            //    .HasForeignKey(m => m.SenderId)
//            //    .OnDelete(DeleteBehavior.Restrict);

//            //modelBuilder.Entity<Message>()
//            //    .HasOne(m => m.Receiver)
//            //    .WithMany(u => u.ReceivedMessages)
//            //    .HasForeignKey(m => m.ReceiverId)
//            //    .OnDelete(DeleteBehavior.Restrict);

//            base.OnModelCreating(modelBuilder);
//        }
//    }
//}


using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

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
            base.OnModelCreating(modelBuilder);

            // ----- TutorModule: Composite Key -----
            modelBuilder.Entity<TutorModule>()
                .HasKey(tm => new { tm.TutorId, tm.ModuleId });

            modelBuilder.Entity<TutorModule>()
                .HasOne(tm => tm.Tutor)
                .WithMany(t => t.TutorModules)
                .HasForeignKey(tm => tm.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TutorModule>()
                .HasOne(tm => tm.Module)
                .WithMany(m => m.TutorModules)
                .HasForeignKey(tm => tm.ModuleId)
                .OnDelete(DeleteBehavior.NoAction);

            // ----- Session relationships -----
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Tutor)
                .WithMany(t => t.Sessions)
                .HasForeignKey(s => s.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Sessions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Module)
                .WithMany(m => m.Sessions)
                .HasForeignKey(s => s.ModuleId)
                .OnDelete(DeleteBehavior.NoAction);

            // ----- ChatMessage relationships -----
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            // ----- Booking relationships -----
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tutor)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Student)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Module)
                .WithMany(m => m.Bookings)
                .HasForeignKey(b => b.ModuleId)
                .OnDelete(DeleteBehavior.NoAction);

            // ----- Optional: enforce DeleteBehavior.NoAction globally -----
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}
