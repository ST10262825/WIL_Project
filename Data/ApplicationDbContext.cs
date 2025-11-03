using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Your existing DbSets remain the same
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<TutorModule> TutorModules { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<GamificationProfile> GamificationProfiles { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<VirtualLearningSpace> VirtualLearningSpaces { get; set; }
        public DbSet<SpaceItem> SpaceItems { get; set; }
        public DbSet<LearningMaterial> LearningMaterials { get; set; }
        public DbSet<LearningMaterialFolder> LearningMaterialFolders { get; set; }
        public DbSet<StudentMaterialAccess> StudentMaterialAccesses { get; set; }
        public DbSet<ChatbotConversation> ChatbotConversations { get; set; }
        public DbSet<ChatbotMessage> ChatbotMessages { get; set; }
        public DbSet<KnowledgeBaseDocument> KnowledgeBaseDocuments { get; set; }
        public DbSet<ChatbotSuggestion> ChatbotSuggestions { get; set; }
        public DbSet<Course> Courses { get; set; }

        // ===========================================================================
        // 🆕 ADD THIS METHOD FOR AZURE SQL CONNECTION
        // ===========================================================================
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This allows EF Core Tools to work with Azure SQL
                // It will use the connection string from appsettings.json
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }

        // Your existing OnModelCreating method remains exactly the same
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- TutorModule: Composite Key -----
            modelBuilder.Entity<TutorModule>(entity =>
            {
                entity.HasKey(tm => new { tm.TutorId, tm.ModuleId });

                entity.HasOne(tm => tm.Tutor)
                    .WithMany(t => t.TutorModules)
                    .HasForeignKey(tm => tm.TutorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(tm => tm.Module)
                    .WithMany(m => m.TutorModules)
                    .HasForeignKey(tm => tm.ModuleId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(c => c.CourseId);
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.HasOne(m => m.Course)
                    .WithMany(c => c.Modules)
                    .HasForeignKey(m => m.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Tutor -> Course  
            modelBuilder.Entity<Tutor>(entity =>
            {
                entity.HasOne(t => t.Course)
                    .WithMany(c => c.Tutors)
                    .HasForeignKey(t => t.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Student -> Course
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasOne(s => s.Course)
                    .WithMany(c => c.Students)
                    .HasForeignKey(s => s.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ----- Session relationships -----
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasOne(s => s.Tutor)
                    .WithMany(t => t.Sessions)
                    .HasForeignKey(s => s.TutorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(s => s.Student)
                    .WithMany(st => st.Sessions)
                    .HasForeignKey(s => s.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Module)
                    .WithMany(m => m.Sessions)
                    .HasForeignKey(s => s.ModuleId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ----- ChatMessage relationships -----
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne(c => c.Sender)
                    .WithMany()
                    .HasForeignKey(c => c.SenderId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(c => c.Receiver)
                    .WithMany()
                    .HasForeignKey(c => c.ReceiverId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ----- Booking relationships -----
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Tutor)
                    .WithMany(t => t.Bookings)
                    .HasForeignKey(b => b.TutorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(b => b.Student)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(b => b.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(b => b.Module)
                    .WithMany(m => m.Bookings)
                    .HasForeignKey(b => b.ModuleId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ----- Review relationships -----
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Booking)
                    .WithOne(b => b.Review)
                    .HasForeignKey<Review>(r => r.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Student)
                    .WithMany(s => s.Reviews)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Restrict); // ✅ Prevent multiple cascade paths
            });

            // ----- Gamification -----
            modelBuilder.Entity<GamificationProfile>(entity =>
            {
                entity.HasKey(gp => gp.GamificationProfileId);
                entity.HasOne(gp => gp.User)
                    .WithOne()
                    .HasForeignKey<GamificationProfile>(gp => gp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserAchievement>(entity =>
            {
                entity.HasKey(ua => ua.UserAchievementId);
                entity.HasOne(ua => ua.GamificationProfile)
                    .WithMany(gp => gp.Achievements)
                    .HasForeignKey(ua => ua.GamificationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ua => ua.Achievement)
                    .WithMany()
                    .HasForeignKey(ua => ua.AchievementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- Virtual Learning Spaces -----
            modelBuilder.Entity<VirtualLearningSpace>(entity =>
            {
                entity.HasKey(vls => vls.SpaceId);
                entity.HasOne(vls => vls.User)
                    .WithOne()
                    .HasForeignKey<VirtualLearningSpace>(vls => vls.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SpaceItem>(entity =>
            {
                entity.HasKey(si => si.ItemId);
                entity.HasOne<VirtualLearningSpace>()
                    .WithMany(vls => vls.Items)
                    .HasForeignKey(si => si.SpaceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- LearningMaterialFolders -----
            modelBuilder.Entity<LearningMaterialFolder>(entity =>
            {
                entity.HasKey(f => f.FolderId);
                entity.HasOne(f => f.Tutor)
                    .WithMany(t => t.LearningMaterialFolders)
                    .HasForeignKey(f => f.TutorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.ParentFolder)
                    .WithMany(f => f.Subfolders)
                    .HasForeignKey(f => f.ParentFolderId)
                    .OnDelete(DeleteBehavior.Restrict); // EF Core will handle deletion
            });

            // ----- LearningMaterials -----
            modelBuilder.Entity<LearningMaterial>(entity =>
            {
                entity.HasKey(lm => lm.LearningMaterialId);
                entity.HasOne(lm => lm.Tutor)
                    .WithMany(t => t.LearningMaterials)
                    .HasForeignKey(lm => lm.TutorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(lm => lm.Folder)
                    .WithMany(f => f.Materials)
                    .HasForeignKey(lm => lm.FolderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- StudentMaterialAccesses -----
            modelBuilder.Entity<StudentMaterialAccess>(entity =>
            {
                entity.HasKey(sma => sma.AccessId);
                entity.HasOne(sma => sma.Student)
                   .WithMany(s => s.MaterialAccesses)
                    .HasForeignKey(sma => sma.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(sma => sma.LearningMaterial)
                    .WithMany(lm => lm.StudentAccesses)
                    .HasForeignKey(sma => sma.LearningMaterialId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(sma => sma.Booking)
                    .WithMany()
                    .HasForeignKey(sma => sma.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(sma => new { sma.StudentId, sma.LearningMaterialId })
                    .IsUnique();
            });

            // ----- Indexes -----
            modelBuilder.Entity<LearningMaterial>()
                .HasIndex(lm => new { lm.TutorId, lm.IsPublic });

            modelBuilder.Entity<LearningMaterialFolder>()
                .HasIndex(f => f.TutorId);

            // ChatbotConversation
            modelBuilder.Entity<ChatbotConversation>(entity =>
            {
                entity.HasKey(cc => cc.ConversationId);
                entity.HasOne(cc => cc.User)
                    .WithMany()
                    .HasForeignKey(cc => cc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ChatbotMessage
            modelBuilder.Entity<ChatbotMessage>(entity =>
            {
                entity.HasKey(cm => cm.MessageId);
                entity.HasOne(cm => cm.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(cm => cm.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // KnowledgeBaseDocument
            modelBuilder.Entity<KnowledgeBaseDocument>(entity =>
            {
                entity.HasKey(kbd => kbd.DocumentId);
            });

            // ChatbotSuggestion
            modelBuilder.Entity<ChatbotSuggestion>(entity =>
            {
                entity.HasKey(cs => cs.SuggestionId);
            });
        }
    }
}