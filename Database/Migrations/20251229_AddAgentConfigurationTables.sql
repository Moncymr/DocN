-- =============================================
-- Agent Configuration System Migration Script
-- Version: 1.0
-- Date: 2025-12-29
-- Description: Adds agent configuration, templates, and usage logging tables
-- =============================================

-- Check if tables already exist before creating
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgentTemplates')
BEGIN
    PRINT 'Creating AgentTemplates table...'
    
    CREATE TABLE AgentTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Icon NVARCHAR(10) NOT NULL DEFAULT '🤖',
        Category NVARCHAR(100) NOT NULL DEFAULT 'General',
        AgentType INT NOT NULL, -- 1=QuestionAnswering, 2=Summarization, 3=Classification, 4=DataExtraction, 5=Comparison, 99=Custom
        
        -- Recommended Configuration
        RecommendedProvider INT NOT NULL, -- 1=Gemini, 2=OpenAI, 3=AzureOpenAI
        RecommendedModel NVARCHAR(100) NULL,
        
        -- Default Prompts and Parameters
        DefaultSystemPrompt NVARCHAR(MAX) NOT NULL,
        DefaultParametersJson NVARCHAR(4000) NOT NULL DEFAULT '{}',
        
        -- Template Metadata
        IsBuiltIn BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        UsageCount INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        
        -- Owner (nullable for system templates)
        OwnerId NVARCHAR(450) NULL,
        
        -- Documentation
        ExampleQuery NVARCHAR(1000) NULL,
        ExampleResponse NVARCHAR(MAX) NULL,
        ConfigurationGuide NVARCHAR(MAX) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_AgentTemplates_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    )
    
    -- Indexes for AgentTemplates
    CREATE INDEX IX_AgentTemplates_Category ON AgentTemplates(Category)
    CREATE INDEX IX_AgentTemplates_AgentType ON AgentTemplates(AgentType)
    CREATE INDEX IX_AgentTemplates_IsBuiltIn ON AgentTemplates(IsBuiltIn)
    CREATE INDEX IX_AgentTemplates_IsActive ON AgentTemplates(IsActive)
    
    PRINT 'AgentTemplates table created successfully'
END
ELSE
BEGIN
    PRINT 'AgentTemplates table already exists, skipping creation'
END
GO

-- Create AgentConfigurations table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgentConfigurations')
BEGIN
    PRINT 'Creating AgentConfigurations table...'
    
    CREATE TABLE AgentConfigurations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Basic Information
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        AgentType INT NOT NULL, -- 1=QuestionAnswering, 2=Summarization, 3=Classification, 4=DataExtraction, 5=Comparison, 99=Custom
        
        -- Provider Configuration
        PrimaryProvider INT NOT NULL, -- 1=Gemini, 2=OpenAI, 3=AzureOpenAI
        FallbackProvider INT NULL,
        
        -- Model Configuration
        ModelName NVARCHAR(100) NULL,
        EmbeddingModelName NVARCHAR(100) NULL,
        
        -- RAG Configuration
        MaxDocumentsToRetrieve INT NOT NULL DEFAULT 5,
        SimilarityThreshold FLOAT NOT NULL DEFAULT 0.7,
        MaxTokensForContext INT NOT NULL DEFAULT 4000,
        MaxTokensForResponse INT NOT NULL DEFAULT 2000,
        Temperature FLOAT NOT NULL DEFAULT 0.7,
        
        -- System Prompt and Instructions
        SystemPrompt NVARCHAR(MAX) NOT NULL,
        CustomInstructions NVARCHAR(2000) NULL,
        
        -- Agent Capabilities
        CanRetrieveDocuments BIT NOT NULL DEFAULT 1,
        CanClassifyDocuments BIT NOT NULL DEFAULT 0,
        CanExtractTags BIT NOT NULL DEFAULT 0,
        CanSummarize BIT NOT NULL DEFAULT 1,
        CanAnswer BIT NOT NULL DEFAULT 1,
        
        -- Search Configuration
        UseHybridSearch BIT NOT NULL DEFAULT 1,
        HybridSearchAlpha FLOAT NOT NULL DEFAULT 0.5,
        
        -- Advanced Options
        EnableConversationHistory BIT NOT NULL DEFAULT 1,
        MaxConversationHistoryMessages INT NOT NULL DEFAULT 10,
        EnableCitation BIT NOT NULL DEFAULT 1,
        EnableStreaming BIT NOT NULL DEFAULT 0,
        
        -- Filters and Scope
        CategoryFilter NVARCHAR(1000) NULL, -- JSON array
        TagFilter NVARCHAR(1000) NULL, -- JSON array
        VisibilityFilter INT NULL, -- 1=Private, 2=Shared, 3=Organization, 4=Public
        
        -- Performance Tuning
        CacheTTLSeconds INT NULL,
        EnableParallelRetrieval BIT NOT NULL DEFAULT 0,
        
        -- Status and Metadata
        IsActive BIT NOT NULL DEFAULT 1,
        IsPublic BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        LastUsedAt DATETIME2 NULL,
        UsageCount INT NOT NULL DEFAULT 0,
        
        -- Ownership
        OwnerId NVARCHAR(450) NULL,
        TenantId INT NULL,
        TemplateId INT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_AgentConfigurations_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentConfigurations_Tenant FOREIGN KEY (TenantId) 
            REFERENCES Tenants(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentConfigurations_Template FOREIGN KEY (TemplateId) 
            REFERENCES AgentTemplates(Id) ON DELETE SET NULL
    )
    
    -- Indexes for AgentConfigurations
    CREATE INDEX IX_AgentConfigurations_OwnerId ON AgentConfigurations(OwnerId)
    CREATE INDEX IX_AgentConfigurations_TenantId ON AgentConfigurations(TenantId)
    CREATE INDEX IX_AgentConfigurations_AgentType ON AgentConfigurations(AgentType)
    CREATE INDEX IX_AgentConfigurations_IsActive ON AgentConfigurations(IsActive)
    CREATE INDEX IX_AgentConfigurations_TenantId_IsActive ON AgentConfigurations(TenantId, IsActive)
    
    PRINT 'AgentConfigurations table created successfully'
END
ELSE
BEGIN
    PRINT 'AgentConfigurations table already exists, skipping creation'
END
GO

-- Create AgentUsageLogs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgentUsageLogs')
BEGIN
    PRINT 'Creating AgentUsageLogs table...'
    
    CREATE TABLE AgentUsageLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Agent Reference
        AgentConfigurationId INT NOT NULL,
        
        -- Query Information
        Query NVARCHAR(MAX) NOT NULL,
        Response NVARCHAR(MAX) NULL,
        
        -- Performance Metrics (stored as BIGINT ticks, then converted to TimeSpan in code)
        RetrievalTimeTicks BIGINT NOT NULL DEFAULT 0,
        SynthesisTimeTicks BIGINT NOT NULL DEFAULT 0,
        TotalTimeTicks BIGINT NOT NULL DEFAULT 0,
        DocumentsRetrieved INT NOT NULL DEFAULT 0,
        
        -- Token Usage
        PromptTokens INT NULL,
        CompletionTokens INT NULL,
        TotalTokens INT NULL,
        
        -- Provider Used
        ProviderUsed INT NOT NULL,
        ModelUsed NVARCHAR(100) NULL,
        
        -- Quality Metrics
        RelevanceScore FLOAT NULL,
        UserFeedbackPositive BIT NULL,
        UserFeedbackComment NVARCHAR(1000) NULL,
        
        -- Error Tracking
        IsError BIT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(MAX) NULL,
        
        -- User and Tenant
        UserId NVARCHAR(450) NULL,
        TenantId INT NULL,
        
        -- Timestamp
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Foreign Keys
        CONSTRAINT FK_AgentUsageLogs_Agent FOREIGN KEY (AgentConfigurationId) 
            REFERENCES AgentConfigurations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AgentUsageLogs_User FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentUsageLogs_Tenant FOREIGN KEY (TenantId) 
            REFERENCES Tenants(Id) ON DELETE SET NULL
    )
    
    -- Indexes for AgentUsageLogs (for analytics queries)
    CREATE INDEX IX_AgentUsageLogs_AgentConfigurationId ON AgentUsageLogs(AgentConfigurationId)
    CREATE INDEX IX_AgentUsageLogs_UserId ON AgentUsageLogs(UserId)
    CREATE INDEX IX_AgentUsageLogs_TenantId ON AgentUsageLogs(TenantId)
    CREATE INDEX IX_AgentUsageLogs_CreatedAt ON AgentUsageLogs(CreatedAt)
    CREATE INDEX IX_AgentUsageLogs_AgentId_CreatedAt ON AgentUsageLogs(AgentConfigurationId, CreatedAt)
    
    PRINT 'AgentUsageLogs table created successfully'
END
ELSE
BEGIN
    PRINT 'AgentUsageLogs table already exists, skipping creation'
END
GO

PRINT '========================================='
PRINT 'Migration completed successfully!'
PRINT 'Agent configuration system tables created'
PRINT '========================================='
GO
