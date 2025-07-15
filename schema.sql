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
CREATE TABLE [Modules] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Modules] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Course] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Tutors] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Surname] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Bio] nvarchar(max) NOT NULL,
    [ProfilePicUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Tutors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tutors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Sessions] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [TutorId] int NOT NULL,
    [ModuleId] int NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sessions_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Sessions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Sessions_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TutorModules] (
    [TutorId] int NOT NULL,
    [ModuleId] int NOT NULL,
    CONSTRAINT [PK_TutorModules] PRIMARY KEY ([TutorId], [ModuleId]),
    CONSTRAINT [FK_TutorModules_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TutorModules_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Sessions_ModuleId] ON [Sessions] ([ModuleId]);

CREATE INDEX [IX_Sessions_StudentId] ON [Sessions] ([StudentId]);

CREATE INDEX [IX_Sessions_TutorId] ON [Sessions] ([TutorId]);

CREATE INDEX [IX_Students_UserId] ON [Students] ([UserId]);

CREATE INDEX [IX_TutorModules_ModuleId] ON [TutorModules] ([ModuleId]);

CREATE INDEX [IX_Tutors_UserId] ON [Tutors] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516133945_InitialCreateFixed', N'9.0.3');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516201830_seedData', N'9.0.3');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tutors]') AND [c].[name] = N'ProfilePicUrl');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tutors] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Tutors] ALTER COLUMN [ProfilePicUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516210643_profilePic', N'9.0.3');

CREATE TABLE [Availabilities] (
    [Id] int NOT NULL IDENTITY,
    [TutorId] int NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    CONSTRAINT [PK_Availabilities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Availabilities_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Ratings] (
    [Id] int NOT NULL IDENTITY,
    [TutorId] int NOT NULL,
    [StudentId] int NOT NULL,
    [Stars] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [DateRated] datetime2 NOT NULL,
    CONSTRAINT [PK_Ratings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ratings_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Ratings_Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [Tutors] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Availabilities_TutorId] ON [Availabilities] ([TutorId]);

CREATE INDEX [IX_Ratings_StudentId] ON [Ratings] ([StudentId]);

CREATE INDEX [IX_Ratings_TutorId] ON [Ratings] ([TutorId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250518143638_AddNotificationAndAvailability', N'9.0.3');

ALTER TABLE [Users] ADD [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Users] ADD [VerificationToken] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250519185528_verifyEmail', N'9.0.3');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tutors]') AND [c].[name] = N'ProfilePicUrl');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tutors] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Tutors] DROP COLUMN [ProfilePicUrl];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250524135914_checkAdmin', N'9.0.3');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250525084837_tutorModule', N'9.0.3');

ALTER TABLE [Tutors] ADD [IsBlocked] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250525102439_UpdateTutor', N'9.0.3');

COMMIT;
GO

