-- =============================================
-- Script: SetupDatabase.sql
-- Description: Script master per creare tutte le tabelle e configurare il database DocN
-- =============================================

USE [DocN]; -- Cambia con il nome del tuo database
GO

PRINT '========================================';
PRINT 'Setup Database DocN';
PRINT '========================================';
PRINT '';

-- 1. Crea tabelle ASP.NET Identity
PRINT '📋 FASE 1: Creazione tabelle Identity';
PRINT '========================================';
:r 01_CreateIdentityTables.sql
PRINT '';

-- 2. Crea tabelle Documents
PRINT '📋 FASE 2: Creazione tabelle documenti';
PRINT '========================================';
:r 02_CreateDocumentTables.sql
PRINT '';

-- 3. Configura Full-Text Search
PRINT '📋 FASE 3: Configurazione Full-Text Search';
PRINT '========================================';
:r 03_ConfigureFullTextSearch.sql
PRINT '';

PRINT '========================================';
PRINT '✅ Setup Database DocN completato!';
PRINT '========================================';
GO
