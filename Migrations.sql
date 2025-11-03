IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Achievements] (
        [AchievementId] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IconUrl] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [PointsReward] int NOT NULL,
        [Criteria] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Achievements] PRIMARY KEY ([AchievementId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [ChatbotSuggestions] (
        [SuggestionId] int NOT NULL IDENTITY,
        [Question] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [UsageCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ChatbotSuggestions] PRIMARY KEY ([SuggestionId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Courses] (
        [CourseId] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([CourseId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [KnowledgeBaseDocuments] (
        [DocumentId] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [DocumentType] nvarchar(max) NULL,
        [Category] nvarchar(max) NULL,
        [Tags] nvarchar(max) NULL,
        [Embedding] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_KnowledgeBaseDocuments] PRIMARY KEY ([DocumentId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] int NOT NULL IDENTITY,
        [Email] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsEmailVerified] bit NOT NULL,
        [VerificationToken] nvarchar(max) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Modules] (
        [ModuleId] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CourseId] int NOT NULL,
        CONSTRAINT [PK_Modules] PRIMARY KEY ([ModuleId]),
        CONSTRAINT [FK_Modules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [ChatbotConversations] (
        [ConversationId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ChatbotConversations] PRIMARY KEY ([ConversationId]),
        CONSTRAINT [FK_ChatbotConversations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [ChatMessageId] int NOT NULL IDENTITY,
        [SenderId] int NOT NULL,
        [ReceiverId] int NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([ChatMessageId]),
        CONSTRAINT [FK_ChatMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_ChatMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [GamificationProfiles] (
        [GamificationProfileId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ExperiencePoints] int NOT NULL,
        [Level] int NOT NULL,
        [CurrentRank] nvarchar(max) NOT NULL,
        [StreakCount] int NOT NULL,
        [LastActivityDate] datetime2 NOT NULL,
        CONSTRAINT [PK_GamificationProfiles] PRIMARY KEY ([GamificationProfileId]),
        CONSTRAINT [FK_GamificationProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Students] (
        [StudentId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CourseId] int NOT NULL,
        [ProfileImage] nvarchar(max) NULL,
        [Bio] nvarchar(max) NULL,
        [IsBlocked] bit NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([StudentId]),
        CONSTRAINT [FK_Students_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Tutors] (
        [TutorId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Surname] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Bio] nvarchar(max) NOT NULL,
        [ProfileImageUrl] nvarchar(max) NULL,
        [AboutMe] nvarchar(max) NULL,
        [Expertise] nvarchar(max) NULL,
        [Education] nvarchar(max) NULL,
        [IsBlocked] bit NOT NULL,
        [CourseId] int NOT NULL,
        [AverageRating] float NOT NULL,
        [TotalReviews] int NOT NULL,
        [RatingCount1] int NOT NULL,
        [RatingCount2] int NOT NULL,
        [RatingCount3] int NOT NULL,
        [RatingCount4] int NOT NULL,
        [RatingCount5] int NOT NULL,
        CONSTRAINT [PK_Tutors] PRIMARY KEY ([TutorId]),
        CONSTRAINT [FK_Tutors_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tutors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [VirtualLearningSpaces] (
        [SpaceId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Theme] nvarchar(max) NOT NULL,
        [Background] nvarchar(max) NULL,
        [IsPublic] bit NOT NULL,
        CONSTRAINT [PK_VirtualLearningSpaces] PRIMARY KEY ([SpaceId]),
        CONSTRAINT [FK_VirtualLearningSpaces_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [ChatbotMessages] (
        [MessageId] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [IsUserMessage] bit NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [MessageType] nvarchar(max) NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_ChatbotMessages] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_ChatbotMessages_ChatbotConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [ChatbotConversations] ([ConversationId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [UserAchievements] (
        [UserAchievementId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [AchievementId] int NOT NULL,
        [EarnedAt] datetime2 NOT NULL,
        [Progress] int NOT NULL,
        [GamificationProfileId] int NOT NULL,
        CONSTRAINT [PK_UserAchievements] PRIMARY KEY ([UserAchievementId]),
        CONSTRAINT [FK_UserAchievements_Achievements_AchievementId] FOREIGN KEY ([AchievementId]) REFERENCES [Achievements] ([AchievementId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserAchievements_GamificationProfiles_GamificationProfileId] FOREIGN KEY ([GamificationProfileId]) REFERENCES [GamificationProfiles] ([GamificationProfileId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Enrollment] (
        [EnrollmentId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CourseId] int NOT NULL,
        [CompletedSessions] int NOT NULL,
        [TotalSessions] int NOT NULL,
        CONSTRAINT [PK_Enrollment] PRIMARY KEY ([EnrollmentId]),
        CONSTRAINT [FK_Enrollment_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Enrollment_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Bookings] (
        [BookingId] int NOT NULL IDENTITY,
        [TutorId] int NOT NULL,
        [StudentId] int NOT NULL,
        [ModuleId] int NOT NULL,
        [ReviewId] int NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Notes] nvarchar(500) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
        CONSTRAINT [FK_Bookings_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([ModuleId]),
        CONSTRAINT [FK_Bookings_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]),
        CONSTRAINT [FK_Bookings_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [LearningMaterialFolders] (
        [FolderId] int NOT NULL IDENTITY,
        [TutorId] int NOT NULL,
        [ParentFolderId] int NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [TutorId1] int NULL,
        CONSTRAINT [PK_LearningMaterialFolders] PRIMARY KEY ([FolderId]),
        CONSTRAINT [FK_LearningMaterialFolders_LearningMaterialFolders_ParentFolderId] FOREIGN KEY ([ParentFolderId]) REFERENCES [LearningMaterialFolders] ([FolderId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LearningMaterialFolders_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LearningMaterialFolders_Tutors_TutorId1] FOREIGN KEY ([TutorId1]) REFERENCES [Tutors] ([TutorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Sessions] (
        [SessionId] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [DateTime] datetime2 NOT NULL,
        [StudentId] int NOT NULL,
        [ModuleId] int NOT NULL,
        [TutorId] int NOT NULL,
        [IsCompleted] bit NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([SessionId]),
        CONSTRAINT [FK_Sessions_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([ModuleId]),
        CONSTRAINT [FK_Sessions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Sessions_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [TutorModules] (
        [TutorId] int NOT NULL,
        [ModuleId] int NOT NULL,
        CONSTRAINT [PK_TutorModules] PRIMARY KEY ([TutorId], [ModuleId]),
        CONSTRAINT [FK_TutorModules_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([ModuleId]),
        CONSTRAINT [FK_TutorModules_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [SpaceItems] (
        [ItemId] int NOT NULL IDENTITY,
        [SpaceId] int NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Position] nvarchar(max) NOT NULL,
        [UnlockedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SpaceItems] PRIMARY KEY ([ItemId]),
        CONSTRAINT [FK_SpaceItems_VirtualLearningSpaces_SpaceId] FOREIGN KEY ([SpaceId]) REFERENCES [VirtualLearningSpaces] ([SpaceId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [Reviews] (
        [ReviewId] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [TutorId] int NOT NULL,
        [StudentId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsVerified] bit NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_Reviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reviews_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]),
        CONSTRAINT [FK_Reviews_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [LearningMaterials] (
        [LearningMaterialId] int NOT NULL IDENTITY,
        [TutorId] int NOT NULL,
        [FolderId] int NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [FileType] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [IsPublic] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [TutorId1] int NULL,
        CONSTRAINT [PK_LearningMaterials] PRIMARY KEY ([LearningMaterialId]),
        CONSTRAINT [FK_LearningMaterials_LearningMaterialFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [LearningMaterialFolders] ([FolderId]) ON DELETE CASCADE,
        CONSTRAINT [FK_LearningMaterials_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId]),
        CONSTRAINT [FK_LearningMaterials_Tutors_TutorId1] FOREIGN KEY ([TutorId1]) REFERENCES [Tutors] ([TutorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE TABLE [StudentMaterialAccesses] (
        [AccessId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [LearningMaterialId] int NOT NULL,
        [BookingId] int NULL,
        [GrantedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [StudentId1] int NULL,
        CONSTRAINT [PK_StudentMaterialAccesses] PRIMARY KEY ([AccessId]),
        CONSTRAINT [FK_StudentMaterialAccesses_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId] FOREIGN KEY ([LearningMaterialId]) REFERENCES [LearningMaterials] ([LearningMaterialId]),
        CONSTRAINT [FK_StudentMaterialAccesses_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]),
        CONSTRAINT [FK_StudentMaterialAccesses_Students_StudentId1] FOREIGN KEY ([StudentId1]) REFERENCES [Students] ([StudentId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Bookings_ModuleId] ON [Bookings] ([ModuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Bookings_StudentId] ON [Bookings] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Bookings_TutorId] ON [Bookings] ([TutorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_ChatbotConversations_UserId] ON [ChatbotConversations] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_ChatbotMessages_ConversationId] ON [ChatbotMessages] ([ConversationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ReceiverId] ON [ChatMessages] ([ReceiverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Enrollment_CourseId] ON [Enrollment] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Enrollment_StudentId] ON [Enrollment] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GamificationProfiles_UserId] ON [GamificationProfiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterialFolders_ParentFolderId] ON [LearningMaterialFolders] ([ParentFolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterialFolders_TutorId] ON [LearningMaterialFolders] ([TutorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterialFolders_TutorId1] ON [LearningMaterialFolders] ([TutorId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterials_FolderId] ON [LearningMaterials] ([FolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterials_TutorId_IsPublic] ON [LearningMaterials] ([TutorId], [IsPublic]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_LearningMaterials_TutorId1] ON [LearningMaterials] ([TutorId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Modules_CourseId] ON [Modules] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Reviews_StudentId] ON [Reviews] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Reviews_TutorId] ON [Reviews] ([TutorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Sessions_ModuleId] ON [Sessions] ([ModuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Sessions_StudentId] ON [Sessions] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Sessions_TutorId] ON [Sessions] ([TutorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_SpaceItems_SpaceId] ON [SpaceItems] ([SpaceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_StudentMaterialAccesses_BookingId] ON [StudentMaterialAccesses] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_StudentMaterialAccesses_LearningMaterialId] ON [StudentMaterialAccesses] ([LearningMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentMaterialAccesses_StudentId_LearningMaterialId] ON [StudentMaterialAccesses] ([StudentId], [LearningMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_StudentMaterialAccesses_StudentId1] ON [StudentMaterialAccesses] ([StudentId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Students_CourseId] ON [Students] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_TutorModules_ModuleId] ON [TutorModules] ([ModuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_Tutors_CourseId] ON [Tutors] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tutors_UserId] ON [Tutors] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_UserAchievements_AchievementId] ON [UserAchievements] ([AchievementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE INDEX [IX_UserAchievements_GamificationProfileId] ON [UserAchievements] ([GamificationProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VirtualLearningSpaces_UserId] ON [VirtualLearningSpaces] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017180322_Everything'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251017180322_Everything', N'9.0.3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [LearningMaterialFolders] DROP CONSTRAINT [FK_LearningMaterialFolders_Tutors_TutorId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [LearningMaterials] DROP CONSTRAINT [FK_LearningMaterials_Tutors_TutorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [LearningMaterials] DROP CONSTRAINT [FK_LearningMaterials_Tutors_TutorId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [Reviews] DROP CONSTRAINT [FK_Reviews_Students_StudentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [SpaceItems] DROP CONSTRAINT [FK_SpaceItems_VirtualLearningSpaces_SpaceId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [StudentMaterialAccesses] DROP CONSTRAINT [FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [StudentMaterialAccesses] DROP CONSTRAINT [FK_StudentMaterialAccesses_Students_StudentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [StudentMaterialAccesses] DROP CONSTRAINT [FK_StudentMaterialAccesses_Students_StudentId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [UserAchievements] DROP CONSTRAINT [FK_UserAchievements_GamificationProfiles_GamificationProfileId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [VirtualLearningSpaces] DROP CONSTRAINT [FK_VirtualLearningSpaces_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DROP INDEX [IX_StudentMaterialAccesses_StudentId1] ON [StudentMaterialAccesses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DROP INDEX [IX_LearningMaterials_TutorId1] ON [LearningMaterials];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DROP INDEX [IX_LearningMaterialFolders_TutorId1] ON [LearningMaterialFolders];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StudentMaterialAccesses]') AND [c].[name] = N'StudentId1');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [StudentMaterialAccesses] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [StudentMaterialAccesses] DROP COLUMN [StudentId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LearningMaterials]') AND [c].[name] = N'TutorId1');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [LearningMaterials] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [LearningMaterials] DROP COLUMN [TutorId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LearningMaterialFolders]') AND [c].[name] = N'TutorId1');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [LearningMaterialFolders] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [LearningMaterialFolders] DROP COLUMN [TutorId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [LearningMaterials] ADD CONSTRAINT [FK_LearningMaterials_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([TutorId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [SpaceItems] ADD CONSTRAINT [FK_SpaceItems_VirtualLearningSpaces_SpaceId] FOREIGN KEY ([SpaceId]) REFERENCES [VirtualLearningSpaces] ([SpaceId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [StudentMaterialAccesses] ADD CONSTRAINT [FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId] FOREIGN KEY ([LearningMaterialId]) REFERENCES [LearningMaterials] ([LearningMaterialId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [StudentMaterialAccesses] ADD CONSTRAINT [FK_StudentMaterialAccesses_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [UserAchievements] ADD CONSTRAINT [FK_UserAchievements_GamificationProfiles_GamificationProfileId] FOREIGN KEY ([GamificationProfileId]) REFERENCES [GamificationProfiles] ([GamificationProfileId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    ALTER TABLE [VirtualLearningSpaces] ADD CONSTRAINT [FK_VirtualLearningSpaces_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029162817_DeleteCascade'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029162817_DeleteCascade', N'9.0.3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171015_DeleteCourse'
)
BEGIN
    ALTER TABLE [Modules] DROP CONSTRAINT [FK_Modules_Courses_CourseId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171015_DeleteCourse'
)
BEGIN
    ALTER TABLE [Modules] ADD CONSTRAINT [FK_Modules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171015_DeleteCourse'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029171015_DeleteCourse', N'9.0.3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171336_CourseModules'
)
BEGIN
    ALTER TABLE [Modules] DROP CONSTRAINT [FK_Modules_Courses_CourseId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171336_CourseModules'
)
BEGIN
    ALTER TABLE [Modules] ADD CONSTRAINT [FK_Modules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029171336_CourseModules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029171336_CourseModules', N'9.0.3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029184341_AddThemePreferenceToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [ThemePreference] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029184341_AddThemePreferenceToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029184341_AddThemePreferenceToUser', N'9.0.3');
END;

COMMIT;
GO

