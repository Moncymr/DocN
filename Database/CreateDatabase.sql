-- ================================================
-- DocN Database Creation Script
-- Database: DocumentArchive
-- Server: NTSPJ-060-02\SQL2025
-- ================================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DocumentArchive')
BEGIN
    CREATE DATABASE DocumentArchive;
    PRINT 'Database DocumentArchive created successfully.';
END
ELSE
BEGIN
    PRINT 'Database DocumentArchive already exists.';
END
GO

USE DocumentArchive;
GO

-- ================================================
-- Create ASP.NET Core Identity Tables
-- ================================================

-- AspNetRoles Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetRoles' and xtype='U')
BEGIN
    CREATE TABLE AspNetRoles (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
    
    CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
    PRINT 'Table AspNetRoles created.';
END
GO

-- AspNetUsers Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUsers' and xtype='U')
BEGIN
    CREATE TABLE AspNetUsers (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL DEFAULT 0,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
        TwoFactorEnabled BIT NOT NULL DEFAULT 0,
        LockoutEnd DATETIMEOFFSET(7) NULL,
        LockoutEnabled BIT NOT NULL DEFAULT 0,
        AccessFailedCount INT NOT NULL DEFAULT 0,
        -- Custom fields
        FirstName NVARCHAR(MAX) NULL,
        LastName NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastLoginAt DATETIME2(7) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    
    CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX EmailIndex ON AspNetUsers(NormalizedEmail);
    PRINT 'Table AspNetUsers created.';
END
GO

-- AspNetUserClaims Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserClaims' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserClaims (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims(UserId);
    PRINT 'Table AspNetUserClaims created.';
END
GO

-- AspNetUserLogins Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserLogins' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserLogins (
        LoginProvider NVARCHAR(450) NOT NULL,
        ProviderKey NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins(UserId);
    PRINT 'Table AspNetUserLogins created.';
END
GO

-- AspNetUserRoles Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserRoles' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles(RoleId);
    PRINT 'Table AspNetUserRoles created.';
END
GO

-- AspNetUserTokens Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserTokens' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserTokens (
        UserId NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name NVARCHAR(450) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    PRINT 'Table AspNetUserTokens created.';
END
GO

-- AspNetRoleClaims Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetRoleClaims' and xtype='U')
BEGIN
    CREATE TABLE AspNetRoleClaims (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims(RoleId);
    PRINT 'Table AspNetRoleClaims created.';
END
GO

-- ================================================
-- Create Application Tables
-- ================================================

-- Documents Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
BEGIN
    CREATE TABLE Documents (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(MAX) NOT NULL,
        FileSize BIGINT NOT NULL,
        ExtractedText NVARCHAR(MAX) NOT NULL,
        SuggestedCategory NVARCHAR(MAX) NULL,
        CategoryReasoning NVARCHAR(2000) NULL,
        ActualCategory NVARCHAR(MAX) NULL,
        Visibility INT NOT NULL DEFAULT 0,  -- 0=Private, 1=Shared, 2=Organization, 3=Public
        EmbeddingVector NVARCHAR(MAX) NULL,
        UploadedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastAccessedAt DATETIME2(7) NULL,
        AccessCount INT NOT NULL DEFAULT 0,
        OwnerId NVARCHAR(450) NULL,  -- Nullable to support documents without authentication
        CONSTRAINT FK_Documents_AspNetUsers_OwnerId FOREIGN KEY (OwnerId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    
    -- Indexes for performance with large datasets
    CREATE INDEX IX_Documents_OwnerId ON Documents(OwnerId);
    CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt);
    CREATE INDEX IX_Documents_Visibility ON Documents(Visibility);
    CREATE INDEX IX_Documents_SuggestedCategory ON Documents(SuggestedCategory);
    CREATE INDEX IX_Documents_ActualCategory ON Documents(ActualCategory);
    
    PRINT 'Table Documents created.';
END
GO

-- DocumentShares Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DocumentShares' and xtype='U')
BEGIN
    CREATE TABLE DocumentShares (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentId INT NOT NULL,
        SharedWithUserId NVARCHAR(450) NOT NULL,
        Permission INT NOT NULL DEFAULT 0,  -- 0=Read, 1=Write, 2=Delete
        SharedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        SharedByUserId NVARCHAR(450) NULL,
        CONSTRAINT FK_DocumentShares_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentShares_AspNetUsers_SharedWithUserId FOREIGN KEY (SharedWithUserId) REFERENCES AspNetUsers(Id)
    );
    
    CREATE INDEX IX_DocumentShares_DocumentId ON DocumentShares(DocumentId);
    CREATE INDEX IX_DocumentShares_SharedWithUserId ON DocumentShares(SharedWithUserId);
    
    PRINT 'Table DocumentShares created.';
END
GO

-- DocumentTags Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DocumentTags' and xtype='U')
BEGIN
    CREATE TABLE DocumentTags (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        DocumentId INT NOT NULL,
        CONSTRAINT FK_DocumentTags_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_DocumentTags_DocumentId ON DocumentTags(DocumentId);
    CREATE INDEX IX_DocumentTags_Name ON DocumentTags(Name);
    
    PRINT 'Table DocumentTags created.';
END
GO

-- AIConfigurations Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIConfigurations' and xtype='U')
BEGIN
    CREATE TABLE AIConfigurations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConfigurationName NVARCHAR(100) NOT NULL,
        AzureOpenAIEndpoint NVARCHAR(MAX) NULL,
        AzureOpenAIKey NVARCHAR(500) NULL,
        EmbeddingDeploymentName NVARCHAR(MAX) NULL,
        ChatDeploymentName NVARCHAR(MAX) NULL,
        MaxDocumentsToRetrieve INT NOT NULL DEFAULT 5,
        SimilarityThreshold FLOAT NOT NULL DEFAULT 0.7,
        MaxTokensForContext INT NOT NULL DEFAULT 4000,
        SystemPrompt NVARCHAR(2000) NULL,
        EmbeddingDimensions INT NOT NULL DEFAULT 1536,
        EmbeddingModel NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL
    );
    
    PRINT 'Table AIConfigurations created.';
END
GO

-- ================================================
-- Insert Sample Data (Optional)
-- ================================================

-- Insert default AI configuration
IF NOT EXISTS (SELECT * FROM AIConfigurations WHERE ConfigurationName = 'Default Configuration')
BEGIN
    INSERT INTO AIConfigurations (
        ConfigurationName,
        MaxDocumentsToRetrieve,
        SimilarityThreshold,
        MaxTokensForContext,
        EmbeddingDimensions,
        EmbeddingModel,
        SystemPrompt,
        IsActive,
        CreatedAt
    )
    VALUES (
        'Default Configuration',
        5,
        0.7,
        4000,
        1536,
        'text-embedding-ada-002',
        'You are a helpful assistant that answers questions based on provided documents. Always cite the source documents when providing information.',
        1,
        GETUTCDATE()
    );
    
    PRINT 'Default AI configuration inserted.';
END
GO

-- ================================================
-- Migration Script: Make OwnerId Nullable
-- Run this if you already have the Documents table created
-- ================================================

-- Check if Documents table exists and OwnerId is NOT NULL
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Documents') AND type in (N'U'))
BEGIN
    -- Check if OwnerId column is NOT NULL
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Documents') AND name = 'OwnerId' AND is_nullable = 0)
    BEGIN
        PRINT 'Migrating Documents table to make OwnerId nullable...';
        
        -- Drop the foreign key constraint
        IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Documents_AspNetUsers_OwnerId')
        BEGIN
            ALTER TABLE Documents DROP CONSTRAINT FK_Documents_AspNetUsers_OwnerId;
            PRINT 'Dropped FK constraint FK_Documents_AspNetUsers_OwnerId';
        END
        
        -- Alter column to be nullable
        ALTER TABLE Documents ALTER COLUMN OwnerId NVARCHAR(450) NULL;
        PRINT 'OwnerId column is now nullable';
        
        -- Recreate foreign key with SET NULL on delete
        ALTER TABLE Documents 
        ADD CONSTRAINT FK_Documents_AspNetUsers_OwnerId 
        FOREIGN KEY (OwnerId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL;
        PRINT 'Recreated FK constraint with ON DELETE SET NULL';
        
        PRINT 'Migration completed successfully!';
    END
    ELSE
    BEGIN
        PRINT 'OwnerId is already nullable, no migration needed.';
    END
END
GO

PRINT '================================================';
PRINT 'Database DocumentArchive setup completed successfully!';
PRINT '================================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Configure connection string in appsettings.json';
PRINT '2. Create upload directory: C:\DocumentArchive\Uploads';
PRINT '3. Configure AI settings (Gemini, OpenAI, Azure OpenAI)';
PRINT '4. Run the application with: dotnet run --project DocN.Client';
PRINT '';
GO
