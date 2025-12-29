# Script di Aggiornamento Database - Modello Gemini

## Panoramica

Questo script aggiorna i record esistenti nella tabella `AIConfigurations` per utilizzare il nuovo modello Gemini `gemini-2.0-flash-exp` al posto del deprecato `gemini-1.5-flash`.

## Motivo dell'Aggiornamento

Il modello `gemini-1.5-flash` è stato deprecato da Google e potrebbe non essere più disponibile per i nuovi account o restituire errori 404 (NOT_FOUND). I nuovi modelli raccomandati sono:

- `gemini-2.0-flash-exp` (predefinito)
- `gemini-2.5-flash`
- `gemini-3-flash`

## Esecuzione dello Script

### Per Database Esistenti

Se hai già un database DocN con configurazioni AI esistenti, esegui questo script:

```sql
-- Percorso: Database/UpdateScripts/004_UpdateGeminiDefaultModel.sql
```

### Per Nuovi Database

Se stai creando un nuovo database, lo script `001_AddMultiProviderAIConfiguration.sql` è già stato aggiornato per utilizzare il nuovo modello predefinito.

## Cosa fa lo Script

1. Identifica tutti i record nella tabella `AIConfigurations` che utilizzano ancora `gemini-1.5-flash`
2. Aggiorna questi record impostando `GeminiChatModel = 'gemini-2.0-flash-exp'`
3. Stampa il numero di record aggiornati

## Verifica Post-Aggiornamento

Dopo aver eseguito lo script, verifica che i record siano stati aggiornati correttamente:

```sql
SELECT Id, ConfigurationName, GeminiChatModel, IsActive
FROM AIConfigurations
WHERE GeminiChatModel IS NOT NULL
```

## Note Importanti

- Lo script è **idempotente**: può essere eseguito più volte senza problemi
- Aggiorna **solo** i record che utilizzano `gemini-1.5-flash`
- I record che utilizzano altri modelli non vengono modificati
- Se non hai configurazioni nel database, il codice utilizza automaticamente il nuovo modello predefinito da `appsettings.json`

## Compatibilità con Versioni Precedenti

Il codice mantiene la compatibilità:
- Se hai configurato manualmente un modello diverso (es. `gemini-1.5-pro`), questo non viene modificato
- Puoi continuare a utilizzare modelli personalizzati se necessario
- I nuovi errori 404 ora mostrano messaggi di aiuto con suggerimenti sui modelli disponibili
