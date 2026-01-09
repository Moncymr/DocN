-- =============================================
-- Script: 014_AddUserGroupsAndDocumentSharing.sql
-- Description: Aggiunge tabelle per gruppi utenti e condivisione documenti con gruppi
-- Date: 2026-01-08
-- Migration: 20260108043707_AddUserGroupsAndDocumentGroupShares
-- =============================================
-- ATTENZIONE: Questo script richiede SQL Server 2016 o superiore
-- =============================================

BEGIN TRANSACTION;

-- Crea tabella UserGroups per gestire gruppi di utenti
CREATE TABLE [UserGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [OwnerId] nvarchar(450) NULL,
    [TenantId] int NULL,
    CONSTRAINT [PK_UserGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserGroups_AspNetUsers_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_UserGroups_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE SET NULL
);

-- Crea tabella DocumentGroupShares per condivisione documenti con gruppi
CREATE TABLE [DocumentGroupShares] (
    [Id] int NOT NULL IDENTITY,
    [DocumentId] int NOT NULL,
    [GroupId] int NOT NULL,
    [Permission] int NOT NULL,  -- 0=Read, 1=Write, 2=Delete
    [SharedAt] datetime2 NOT NULL,
    [SharedByUserId] nvarchar(max) NULL,
    CONSTRAINT [PK_DocumentGroupShares] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentGroupShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DocumentGroupShares_UserGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [UserGroups] ([Id]) ON DELETE CASCADE
);

-- Crea tabella UserGroupMembers per membri dei gruppi
CREATE TABLE [UserGroupMembers] (
    [Id] int NOT NULL IDENTITY,
    [GroupId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Role] int NOT NULL,  -- 0=Member, 1=Admin
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserGroupMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserGroupMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserGroupMembers_UserGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [UserGroups] ([Id]) ON DELETE CASCADE
);

-- Crea indici per performance e constraint unique
CREATE UNIQUE INDEX [IX_DocumentGroupShares_DocumentId_GroupId] ON [DocumentGroupShares] ([DocumentId], [GroupId]);
CREATE INDEX [IX_DocumentGroupShares_GroupId] ON [DocumentGroupShares] ([GroupId]);

CREATE UNIQUE INDEX [IX_UserGroupMembers_GroupId_UserId] ON [UserGroupMembers] ([GroupId], [UserId]);
CREATE INDEX [IX_UserGroupMembers_UserId] ON [UserGroupMembers] ([UserId]);

CREATE INDEX [IX_UserGroups_Name] ON [UserGroups] ([Name]);
CREATE INDEX [IX_UserGroups_OwnerId] ON [UserGroups] ([OwnerId]);
CREATE INDEX [IX_UserGroups_TenantId] ON [UserGroups] ([TenantId]);

-- Aggiorna tabella __EFMigrationsHistory
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260108043707_AddUserGroupsAndDocumentGroupShares', N'10.0.1');

COMMIT;
GO

-- =============================================
-- Script di verifica
-- =============================================
-- Verifica che le tabelle siano state create correttamente
SELECT 
    'UserGroups' as TableName,
    COUNT(*) as RecordCount
FROM [UserGroups]
UNION ALL
SELECT 
    'DocumentGroupShares' as TableName,
    COUNT(*) as RecordCount
FROM [DocumentGroupShares]
UNION ALL
SELECT 
    'UserGroupMembers' as TableName,
    COUNT(*) as RecordCount
FROM [UserGroupMembers];

-- Verifica indici
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('UserGroups', 'DocumentGroupShares', 'UserGroupMembers')
ORDER BY t.name, i.name;

PRINT 'Script 014 completato con successo!';
PRINT 'Tabelle create: UserGroups, DocumentGroupShares, UserGroupMembers';
PRINT 'Indici creati per ottimizzare le query';
