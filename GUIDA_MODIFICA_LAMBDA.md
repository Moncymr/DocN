# 🎯 Come Modificare il Parametro MMR Lambda

## 📍 Dove Modificare Lambda

Il parametro **MMR Lambda** può essere modificato in **3 modi**:

---

## 1. 🖥️ **Interfaccia Web (Consigliato)** ✅

### Passo 1: Accedi alla Pagina di Configurazione

1. **Accedi** all'applicazione DocN
2. Vai alla pagina **Configurazione AI**:
   - URL: `https://tuo-server/config`
   - Oppure clicca su **⚙️ Configurazione** nel menu

### Passo 2: Trova la Sezione RAG

Scorri fino alla sezione **"🔍 Configurazione RAG"**

### Passo 3: Modifica MMR Lambda

Troverai il campo:
```
🎯 MMR Lambda (Diversità Risultati)
[0.7] ← Modifica questo valore
0.0 - 1.0 | 0.0=Max diversità, 0.7=Bilanciato (consigliato), 1.0=Max rilevanza
```

**Valori suggeriti**:
- **0.3** = Alta diversità (per ricerca esplorativa)
- **0.5** = Bilanciato
- **0.7** = **Consigliato** (70% rilevanza, 30% diversità)
- **0.9** = Alta rilevanza (per ricerca precisa)

### Passo 4: Salva

Clicca sul pulsante **"💾 Salva Configurazione"** in fondo alla pagina.

✅ **Il cambiamento è immediato** - non serve riavviare l'applicazione!

---

## 2. 🗄️ **Direttamente nel Database SQL Server** 

### Opzione A: SQL Server Management Studio (SSMS)

1. Apri **SQL Server Management Studio**
2. Connettiti al tuo database DocN
3. Esegui questa query:

```sql
-- Visualizza configurazione attuale
SELECT 
    Id,
    ConfigurationName,
    MMRLambda,
    IsActive
FROM AIConfigurations
WHERE IsActive = 1;

-- Modifica lambda per configurazione attiva
UPDATE AIConfigurations
SET MMRLambda = 0.7  -- Cambia questo valore (0.0 - 1.0)
WHERE IsActive = 1;
```

### Opzione B: Azure Data Studio

1. Apri **Azure Data Studio**
2. Connettiti al database
3. Esegui la stessa query sopra

### Opzione C: Linea di Comando (sqlcmd)

```bash
sqlcmd -S YOUR_SERVER -d DocNDb -Q "UPDATE AIConfigurations SET MMRLambda = 0.7 WHERE IsActive = 1"
```

---

## 3. 📝 **File di Configurazione (Fallback)**

Se il database non è disponibile, il sistema usa il valore da `appsettings.json`:

### File: `DocN.Server/appsettings.json`

```json
{
  "EnhancedRAG": {
    "Reranking": {
      "MMRLambda": 0.7
    }
  }
}
```

**⚠️ Nota**: Questo è solo un fallback. Il valore nel database ha **priorità più alta**.

---

## 📊 Guida ai Valori Lambda

| Valore | Caso d'Uso | Risultato |
|--------|-----------|-----------|
| **0.0 - 0.3** | Ricerca esplorativa, brainstorming | Massima varietà, documenti molto diversi |
| **0.4 - 0.6** | Ricerca bilanciata | Buon mix di rilevanza e diversità |
| **0.7** | **Default - Q&A generale** | **70% rilevanza, 30% diversità (consigliato)** |
| **0.8 - 0.9** | Ricerca precisa (legale, tecnica) | Alta precisione, poca varietà |
| **1.0** | Conformità, audit | Solo documenti più rilevanti, zero diversità |

---

## 🔄 Esempio Pratico: Configurazione per Diversi Scenari

### Scenario 1: Ricerca Legale (Alta Precisione)

**Interfaccia Web**:
1. Vai a `/config`
2. Nella sezione "Configurazione RAG"
3. Imposta **MMR Lambda = 0.9**
4. Salva

**SQL**:
```sql
UPDATE AIConfigurations
SET MMRLambda = 0.9, ConfigurationName = 'Legal - Alta Precisione'
WHERE IsActive = 1;
```

### Scenario 2: Ricerca Creativa (Alta Diversità)

**Interfaccia Web**:
1. Vai a `/config`
2. Nella sezione "Configurazione RAG"
3. Imposta **MMR Lambda = 0.3**
4. Salva

**SQL**:
```sql
UPDATE AIConfigurations
SET MMRLambda = 0.3, ConfigurationName = 'Ricerca Creativa'
WHERE IsActive = 1;
```

### Scenario 3: Configurazioni Multiple per Tenant Diversi

**SQL**:
```sql
-- Tenant 1: Legale (alta precisione)
INSERT INTO AIConfigurations (ConfigurationName, MMRLambda, IsActive)
VALUES ('Tenant_Legal', 0.9, 1);

-- Tenant 2: Marketing (alta diversità)
INSERT INTO AIConfigurations (ConfigurationName, MMRLambda, IsActive)
VALUES ('Tenant_Marketing', 0.4, 1);

-- Tenant 3: Generale (bilanciato)
INSERT INTO AIConfigurations (ConfigurationName, MMRLambda, IsActive)
VALUES ('Tenant_General', 0.7, 1);
```

---

## 🎥 Screenshot della Pagina

### Prima della Modifica:

La pagina `/config` mostra la sezione **Configurazione RAG** con:
- Max Documenti da Recuperare: `5`
- Soglia Similarità: `0.7`
- Max Token per Contesto: `4000`
- **🎯 MMR Lambda (Diversità Risultati): `0.7`** ← Questo campo

### Dopo la Modifica:

1. Cambia il valore nel campo MMR Lambda
2. Clicca "💾 Salva Configurazione"
3. Vedi messaggio: ✅ "Configurazione salvata con successo!"

---

## ✅ Verifica che la Modifica Funzioni

### Metodo 1: Controlla il Database

```sql
SELECT ConfigurationName, MMRLambda, IsActive, UpdatedAt
FROM AIConfigurations
WHERE IsActive = 1;
```

### Metodo 2: Controlla i Log dell'Applicazione

Quando esegui una ricerca, vedrai nei log:

```
[Information] Searching with MMR: topK=10, lambda=0.7 (configured=0.7, database=True)
```

Se vedi `database=True`, significa che il valore viene caricato dal database! ✅

### Metodo 3: Test Pratico

1. **Lambda = 0.9** (alta rilevanza):
   - Esegui una ricerca
   - I risultati saranno molto simili tra loro
   - Alta precisione

2. **Lambda = 0.3** (alta diversità):
   - Esegui la stessa ricerca
   - I risultati saranno molto diversi tra loro
   - Maggiore varietà

---

## 🔧 Risoluzione Problemi

### Problema: Il valore non cambia

**Soluzione 1**: Verifica che la configurazione sia attiva
```sql
SELECT Id, ConfigurationName, IsActive
FROM AIConfigurations;

-- Attiva la configurazione corretta
UPDATE AIConfigurations SET IsActive = 0;  -- Disattiva tutte
UPDATE AIConfigurations SET IsActive = 1 WHERE Id = 1;  -- Attiva quella desiderata
```

**Soluzione 2**: Controlla che il valore sia valido (0.0 - 1.0)
```sql
-- Il valore deve essere tra 0 e 1
UPDATE AIConfigurations
SET MMRLambda = CASE 
    WHEN MMRLambda < 0 THEN 0.0
    WHEN MMRLambda > 1 THEN 1.0
    ELSE MMRLambda
END
WHERE IsActive = 1;
```

### Problema: Campo non visibile nella pagina

**Soluzione**: Esegui la migrazione del database
```bash
sqlcmd -S YOUR_SERVER -d DocNDb -i Database/UpdateScripts/013_AddMMRLambdaConfiguration.sql
```

---

## 📞 Supporto

Se hai problemi:
1. Controlla che la colonna `MMRLambda` esista nel database
2. Verifica che la configurazione sia attiva (`IsActive = 1`)
3. Controlla i log dell'applicazione per errori
4. Consulta `CONFIGURAZIONE_LAMBDA_MMR.md` per dettagli tecnici

---

**Pagina di Riferimento**: `/config` (Configurazione AI)  
**Sezione**: 🔍 Configurazione RAG  
**Campo**: 🎯 MMR Lambda (Diversità Risultati)  
**Range**: 0.0 - 1.0  
**Default**: 0.7 (consigliato)

✅ **Modifica immediata** - nessun riavvio necessario!
