-- ================================================
-- Update Script 013: Add MMR Lambda Configuration
-- SQL Server 2025
-- ================================================
-- Description: Adds MMRLambda column to AIConfigurations table
--              for configurable diversity in search results
-- Date: 2026-01-07
-- ================================================

PRINT '';
PRINT '================================================';
PRINT 'Update 013: Add MMR Lambda Configuration';
PRINT '================================================';
PRINT '';

PRINT 'Inizio aggiornamento tabella AIConfigurations per MMR Lambda...';
PRINT '';

-- Check if column already exists
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') 
               AND name = 'MMRLambda')
BEGIN
    PRINT '  → Aggiunta colonna MMRLambda...';
    
    ALTER TABLE [dbo].[AIConfigurations]
    ADD MMRLambda FLOAT NOT NULL DEFAULT 0.7;
    
    PRINT '  ✓ Colonna MMRLambda aggiunta con successo (default: 0.7)';
    PRINT '';
    PRINT '  Significato valori MMRLambda:';
    PRINT '    • 0.0 = Massima diversità (varietà massima, minima rilevanza)';
    PRINT '    • 0.5 = Bilanciato (50% rilevanza, 50% diversità)';
    PRINT '    • 0.7 = Raccomandato (70% rilevanza, 30% diversità)';
    PRINT '    • 1.0 = Massima rilevanza (nessuna diversità)';
END
ELSE
BEGIN
    PRINT '  ℹ️  Colonna MMRLambda già esistente, skip...';
END

PRINT '';

-- Update existing configurations with recommended default if they have NULL
PRINT '  → Aggiornamento configurazioni esistenti...';

UPDATE [dbo].[AIConfigurations]
SET MMRLambda = 0.7
WHERE MMRLambda IS NULL;

DECLARE @UpdatedRows INT = @@ROWCOUNT;
PRINT '  ✓ Aggiornate ' + CAST(@UpdatedRows AS NVARCHAR(10)) + ' configurazioni con valore default 0.7';

PRINT '';
PRINT '================================================';
PRINT '✅ Update 013 completato con successo!';
PRINT '================================================';
PRINT '';

-- Verification
PRINT 'Verifica modifiche:';
SELECT 
    Id,
    ConfigurationName,
    MMRLambda,
    IsActive
FROM [dbo].[AIConfigurations];

PRINT '';
PRINT 'Per maggiori informazioni sul parametro MMR Lambda:';
PRINT '  • Documentazione: CONFIGURAZIONE_LAMBDA_MMR.md';
PRINT '  • Formula MMR: Score = λ × Rilevanza - (1-λ) × max(Similarità con documenti già selezionati)';
PRINT '';

GO
