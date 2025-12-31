using DocN.Data.Models;
using DocN.Core.Interfaces;
using DocN.Data.Utilities;
using Microsoft.EntityFrameworkCore;

namespace DocN.Data.Services;

/// <summary>
/// Interfaccia per il servizio di gestione documenti.
/// Fornisce operazioni CRUD, condivisione, controllo accessi e gestione visibilità.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Recupera un documento specifico se l'utente ha i permessi di accesso.
    /// </summary>
    /// <param name="documentId">ID del documento da recuperare</param>
    /// <param name="userId">ID dell'utente richiedente</param>
    /// <returns>Il documento se trovato e accessibile, altrimenti null</returns>
    Task<Document?> GetDocumentAsync(int documentId, string userId);
    
    /// <summary>
    /// Verifica se un utente ha i permessi per accedere a un documento.
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>True se l'utente può accedere al documento, altrimenti false</returns>
    Task<bool> CanUserAccessDocument(int documentId, string userId);
    
    /// <summary>
    /// Ottiene lista paginata di documenti accessibili all'utente.
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="page">Numero pagina (inizia da 1)</param>
    /// <param name="pageSize">Numero di documenti per pagina</param>
    /// <returns>Lista documenti accessibili all'utente</returns>
    Task<List<Document>> GetUserDocumentsAsync(string userId, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Conta il numero totale di documenti accessibili all'utente.
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Numero totale di documenti accessibili</returns>
    Task<int> GetTotalDocumentCountAsync(string userId);
    
    /// <summary>
    /// Scarica il file di un documento se l'utente ha i permessi.
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Array di byte del file se accessibile, altrimenti null</returns>
    Task<byte[]?> DownloadDocumentAsync(int documentId, string userId);
    
    /// <summary>
    /// Condivide un documento con un altro utente.
    /// </summary>
    /// <param name="documentId">ID del documento da condividere</param>
    /// <param name="shareWithUserId">ID dell'utente con cui condividere</param>
    /// <param name="permission">Livello di permesso da assegnare</param>
    /// <param name="currentUserId">ID dell'utente proprietario che condivide</param>
    /// <returns>True se condivisione riuscita, altrimenti false</returns>
    Task<bool> ShareDocumentAsync(int documentId, string shareWithUserId, DocumentPermission permission, string currentUserId);
    
    /// <summary>
    /// Aggiorna il livello di visibilità di un documento.
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="visibility">Nuovo livello di visibilità</param>
    /// <param name="userId">ID dell'utente proprietario</param>
    /// <returns>True se aggiornamento riuscito, altrimenti false</returns>
    Task<bool> UpdateDocumentVisibilityAsync(int documentId, DocumentVisibility visibility, string userId);
    
    /// <summary>
    /// Crea un nuovo documento nel database con validazione embedding.
    /// </summary>
    /// <param name="document">Documento da creare</param>
    /// <returns>Documento creato con ID assegnato</returns>
    Task<Document> CreateDocumentAsync(Document document);
    
    /// <summary>
    /// Aggiorna un documento esistente se l'utente è il proprietario.
    /// </summary>
    /// <param name="document">Documento con dati aggiornati</param>
    /// <param name="userId">ID dell'utente richiedente</param>
    /// <returns>Documento aggiornato</returns>
    Task<Document> UpdateDocumentAsync(Document document, string userId);
    
    /// <summary>
    /// Salva relazioni di similarità tra documenti per raccomandazioni.
    /// </summary>
    /// <param name="sourceDocumentId">ID del documento sorgente</param>
    /// <param name="similarDocuments">Lista documenti simili con score</param>
    /// <returns>Task completato</returns>
    Task SaveSimilarDocumentsAsync(int sourceDocumentId, List<RelevantDocumentResult> similarDocuments);
}

/// <summary>
/// Implementazione del servizio di gestione documenti.
/// Gestisce operazioni CRUD, controllo accessi multi-tenant, condivisione e visibilità.
/// </summary>
/// <remarks>
/// Scopo: Fornire layer business logic per operazioni su documenti con controlli di sicurezza.
/// Output: Documenti filtrati per permessi utente, operazioni CRUD sicure.
/// </remarks>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;

    public DocumentService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Recupera un documento con tutte le relazioni (Owner, Shares, Tags) verificando i permessi di accesso.
    /// </summary>
    /// <param name="documentId">ID del documento da recuperare</param>
    /// <param name="userId">ID dell'utente richiedente per controllo accessi</param>
    /// <returns>Documento completo se accessibile, null se non trovato o non autorizzato</returns>
    /// <remarks>
    /// Scopo: Recuperare documento con controllo sicurezza integrato.
    /// Output: Document con navigazioni caricate, o null se inaccessibile.
    /// </remarks>
    public async Task<Document?> GetDocumentAsync(int documentId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Owner)
            .Include(d => d.Shares)
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return null;

        // Check access permissions
        if (!await CanUserAccessDocument(documentId, userId))
            return null;

        return document;
    }

    /// <summary>
    /// Verifica permessi accesso documento considerando: proprietà, visibilità, condivisioni e multi-tenancy.
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="userId">ID dell'utente da verificare</param>
    /// <returns>True se l'utente ha accesso, false altrimenti</returns>
    /// <remarks>
    /// Scopo: Controllo centralizzato permessi con logica multi-tenant.
    /// Logica: Owner → Public → Organization → Shared → Denied
    /// Output: Boolean indicante permesso accesso.
    /// </remarks>
    public async Task<bool> CanUserAccessDocument(int documentId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return false;

        // Owner always has access (if document has an owner)
        if (!string.IsNullOrEmpty(document.OwnerId) && document.OwnerId == userId)
            return true;

        // Documents without owner are accessible based on visibility
        if (string.IsNullOrEmpty(document.OwnerId))
        {
            return document.Visibility == DocumentVisibility.Public || 
                   document.Visibility == DocumentVisibility.Organization;
        }

        // Check visibility settings
        if (document.Visibility == DocumentVisibility.Public)
            return true;

        if (document.Visibility == DocumentVisibility.Organization)
            return true; // In a real app, check if user is in same organization

        // Check if document is shared with user
        if (document.Visibility == DocumentVisibility.Shared)
        {
            return document.Shares.Any(s => s.SharedWithUserId == userId);
        }

        return false;
    }

    /// <summary>
    /// Recupera lista paginata documenti accessibili considerando ownership, condivisioni e tenant.
    /// </summary>
    /// <param name="userId">ID utente (può essere null per documenti pubblici)</param>
    /// <param name="page">Numero pagina (default 1, base 1)</param>
    /// <param name="pageSize">Elementi per pagina (default 20)</param>
    /// <returns>Lista documenti ordinata per data upload (più recenti prima)</returns>
    /// <remarks>
    /// Scopo: Lista documenti con filtro permessi automatico e paginazione.
    /// Filtri applicati: Owner OR Shared OR SameTenant OR Public
    /// Output: Lista Document con Include Owner e Tags.
    /// </remarks>
    public async Task<List<Document>> GetUserDocumentsAsync(string userId, int page = 1, int pageSize = 20)
    {
        // Get documents owned by user, shared with user, or in same tenant
        var query = _context.Documents.AsQueryable();
        
        if (string.IsNullOrEmpty(userId))
        {
            // If no user ID, return all public documents and documents without owner
            query = query.Where(d => d.Visibility == DocumentVisibility.Public || d.OwnerId == null);
        }
        else
        {
            // Get user's tenant
            var user = await _context.Users.FindAsync(userId);
            var userTenantId = user?.TenantId;
            
            // Get documents owned by user, shared with user, OR in the same tenant (if user has a tenant)
            query = _context.Documents.Where(d => 
                d.OwnerId == userId ||  // Owned by user
                d.OwnerId == null ||    // No owner (accessible to all in tenant)
                d.Shares.Any(s => s.SharedWithUserId == userId) ||  // Shared with user
                (userTenantId != null && d.TenantId == userTenantId) // Same tenant
            );
        }

        var allDocs = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(d => d.Owner)
            .Include(d => d.Tags)
            .ToListAsync();

        return allDocs;
    }

    /// <summary>
    /// Conta documenti totali accessibili all'utente (per paginazione UI).
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <returns>Numero totale documenti accessibili</returns>
    /// <remarks>
    /// Scopo: Calcolare total count per paginazione mantenendo stessi filtri di GetUserDocumentsAsync.
    /// Output: Intero con count documenti.
    /// </remarks>
    public async Task<int> GetTotalDocumentCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            // If no user ID, count all public documents and documents without owner
            return await _context.Documents.CountAsync(d => d.Visibility == DocumentVisibility.Public || d.OwnerId == null);
        }
        
        // Get user's tenant
        var user = await _context.Users.FindAsync(userId);
        var userTenantId = user?.TenantId;
        
        // Count documents owned by user, shared with user, OR in the same tenant
        return await _context.Documents.CountAsync(d => 
            d.OwnerId == userId ||  // Owned by user
            d.OwnerId == null ||    // No owner (accessible to all in tenant)
            d.Shares.Any(s => s.SharedWithUserId == userId) ||  // Shared with user
            (userTenantId != null && d.TenantId == userTenantId) // Same tenant
        );
    }

    /// <summary>
    /// Scarica file documento dal filesystem dopo verifica permessi.
    /// </summary>
    /// <param name="documentId">ID documento</param>
    /// <param name="userId">ID utente richiedente</param>
    /// <returns>Byte array del file se accessibile, null se negato o file non esiste</returns>
    /// <remarks>
    /// Scopo: Download sicuro file con controllo accessi.
    /// Verifica: Permessi utente + esistenza file su disco
    /// Output: Byte[] del file o null.
    /// </remarks>
    public async Task<byte[]?> DownloadDocumentAsync(int documentId, string userId)
    {
        // Check if user has access
        if (!await CanUserAccessDocument(documentId, userId))
            return null;

        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || !File.Exists(document.FilePath))
            return null;

        try
        {
            return await File.ReadAllBytesAsync(document.FilePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Condivide documento con altro utente creando o aggiornando DocumentShare.
    /// </summary>
    /// <param name="documentId">ID documento da condividere</param>
    /// <param name="shareWithUserId">ID utente destinatario</param>
    /// <param name="permission">Permesso da assegnare (Read, Write, Admin)</param>
    /// <param name="currentUserId">ID proprietario che condivide</param>
    /// <returns>True se condivisione riuscita, false se negata (solo owner può condividere)</returns>
    /// <remarks>
    /// Scopo: Gestire condivisione granulare documenti.
    /// Logica: Solo owner può condividere, aggiorna visibilità a Shared se era Private.
    /// Output: Boolean successo operazione.
    /// </remarks>
    public async Task<bool> ShareDocumentAsync(int documentId, string shareWithUserId, DocumentPermission permission, string currentUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            return false;

        // Only owner can share (documents without owner cannot be shared)
        if (string.IsNullOrEmpty(document.OwnerId) || document.OwnerId != currentUserId)
            return false;

        // Check if already shared
        var existingShare = await _context.DocumentShares
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SharedWithUserId == shareWithUserId);

        if (existingShare != null)
        {
            existingShare.Permission = permission;
        }
        else
        {
            var share = new DocumentShare
            {
                DocumentId = documentId,
                SharedWithUserId = shareWithUserId,
                Permission = permission,
                SharedByUserId = currentUserId,
                SharedAt = DateTime.UtcNow
            };
            _context.DocumentShares.Add(share);
        }

        // Update document visibility to Shared if it's Private
        if (document.Visibility == DocumentVisibility.Private)
        {
            document.Visibility = DocumentVisibility.Shared;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Modifica livello visibilità documento (Private, Shared, Organization, Public).
    /// </summary>
    /// <param name="documentId">ID documento</param>
    /// <param name="visibility">Nuovo livello visibilità</param>
    /// <param name="userId">ID utente (deve essere owner)</param>
    /// <returns>True se aggiornamento riuscito, false se negato</returns>
    /// <remarks>
    /// Scopo: Permettere a owner di cambiare visibilità documento.
    /// Restrizione: Solo owner può modificare.
    /// Output: Boolean successo operazione.
    /// </remarks>
    public async Task<bool> UpdateDocumentVisibilityAsync(int documentId, DocumentVisibility visibility, string userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            return false;

        // Only owner can change visibility (documents without owner cannot have visibility changed)
        if (string.IsNullOrEmpty(document.OwnerId) || document.OwnerId != userId)
            return false;

        document.Visibility = visibility;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Crea nuovo documento con validazione dimensioni embedding vettoriale.
    /// </summary>
    /// <param name="document">Documento da creare con metadata ed embedding</param>
    /// <returns>Documento creato con ID assegnato</returns>
    /// <exception cref="InvalidOperationException">Se embedding ha dimensioni errate o salvataggio DB fallisce</exception>
    /// <remarks>
    /// Scopo: Inserire documento validando embedding per evitare errori VECTOR SQL Server.
    /// Validazione: Verifica 768 o 1536 dimensioni prima di save.
    /// Output: Document con Id popolato, exception se errore validazione o DB.
    /// </remarks>
    public async Task<Document> CreateDocumentAsync(Document document)
    {
        try
        {
            // Determine which embedding field is populated and validate
            float[]? embeddingToValidate = null;
            
            if (document.EmbeddingVector768 != null && document.EmbeddingVector768.Length > 0)
            {
                embeddingToValidate = document.EmbeddingVector768;
                document.EmbeddingDimension = 768;
            }
            else if (document.EmbeddingVector1536 != null && document.EmbeddingVector1536.Length > 0)
            {
                embeddingToValidate = document.EmbeddingVector1536;
                document.EmbeddingDimension = 1536;
            }
            
            // Validate embedding dimensions before saving to avoid database errors
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embeddingToValidate);
            
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }
        catch (DbUpdateException ex)
        {
            // Extract the inner exception details for better error reporting
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            
            // Check for vector dimension mismatch error
            if (EmbeddingValidationHelper.IsVectorDimensionMismatchError(innerMessage))
            {
                var embeddingDim = document.EmbeddingDimension ?? 0;
                throw new InvalidOperationException(
                    EmbeddingValidationHelper.CreateDimensionMismatchErrorMessage(embeddingDim, innerMessage),
                    ex);
            }
            
            throw new InvalidOperationException($"Database save failed: {innerMessage}", ex);
        }
    }

    /// <summary>
    /// Aggiorna documento esistente con controllo ownership e validazione embedding.
    /// </summary>
    /// <param name="document">Documento con dati aggiornati</param>
    /// <param name="userId">ID utente richiedente (deve essere owner)</param>
    /// <returns>Documento aggiornato</returns>
    /// <exception cref="InvalidOperationException">Se documento non trovato</exception>
    /// <exception cref="UnauthorizedAccessException">Se utente non è owner</exception>
    /// <remarks>
    /// Scopo: Update sicuro con validazione owner e embedding dimensions.
    /// Logica: Carica documento esistente, verifica owner, aggiorna proprietà, valida embedding.
    /// Output: Document aggiornato con tutte proprietà sincronizzate.
    /// </remarks>
    public async Task<Document> UpdateDocumentAsync(Document document, string userId)
    {
        var existingDocument = await _context.Documents
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == document.Id);

        if (existingDocument == null)
            throw new InvalidOperationException($"Document with ID {document.Id} not found");

        // Only owner can update (documents without owner cannot be updated)
        if (string.IsNullOrEmpty(existingDocument.OwnerId) || existingDocument.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the document owner can update this document");

        // Determine which embedding field is populated and validate
        float[]? embeddingToValidate = null;
        
        if (document.EmbeddingVector768 != null && document.EmbeddingVector768.Length > 0)
        {
            embeddingToValidate = document.EmbeddingVector768;
            document.EmbeddingDimension = 768;
        }
        else if (document.EmbeddingVector1536 != null && document.EmbeddingVector1536.Length > 0)
        {
            embeddingToValidate = document.EmbeddingVector1536;
            document.EmbeddingDimension = 1536;
        }
        
        // Validate embedding dimensions before updating
        EmbeddingValidationHelper.ValidateEmbeddingDimensions(embeddingToValidate);

        // Update document properties
        existingDocument.FileName = document.FileName;
        existingDocument.FilePath = document.FilePath;
        existingDocument.ContentType = document.ContentType;
        existingDocument.FileSize = document.FileSize;
        existingDocument.ExtractedText = document.ExtractedText;
        existingDocument.SuggestedCategory = document.SuggestedCategory;
        existingDocument.CategoryReasoning = document.CategoryReasoning;
        existingDocument.ActualCategory = document.ActualCategory;
        existingDocument.Visibility = document.Visibility;
        existingDocument.EmbeddingVector768 = document.EmbeddingVector768;
        existingDocument.EmbeddingVector1536 = document.EmbeddingVector1536;
        existingDocument.EmbeddingDimension = document.EmbeddingDimension;
        existingDocument.Notes = document.Notes;
        existingDocument.PageCount = document.PageCount;
        existingDocument.DetectedLanguage = document.DetectedLanguage;
        existingDocument.ProcessingStatus = document.ProcessingStatus;
        existingDocument.ProcessingError = document.ProcessingError;
        existingDocument.AIAnalysisDate = document.AIAnalysisDate;
        existingDocument.AITagsJson = document.AITagsJson;
        existingDocument.ExtractedMetadataJson = document.ExtractedMetadataJson;

        // Update tags
        if (existingDocument.Tags != null)
        {
            existingDocument.Tags.Clear();
        }
        
        if (document.Tags != null)
        {
            foreach (var tag in document.Tags)
            {
                existingDocument.Tags.Add(new DocumentTag
                {
                    Name = tag.Name,
                    Document = existingDocument
                });
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            return existingDocument;
        }
        catch (DbUpdateException ex)
        {
            // Extract the inner exception details for better error reporting
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            
            // Check for vector dimension mismatch error
            if (EmbeddingValidationHelper.IsVectorDimensionMismatchError(innerMessage))
            {
                var embeddingDim = document.EmbeddingVector?.Length ?? 0;
                throw new InvalidOperationException(
                    EmbeddingValidationHelper.CreateDimensionMismatchErrorMessage(embeddingDim, innerMessage),
                    ex);
            }
            
            throw new InvalidOperationException($"Database save failed: {innerMessage}", ex);
        }
    }

    /// <summary>
    /// Salva relazioni di similarità per documento (max 5 documenti simili).
    /// </summary>
    /// <param name="sourceDocumentId">ID documento sorgente</param>
    /// <param name="similarDocuments">Lista documenti simili ordinati per score</param>
    /// <returns>Task completato</returns>
    /// <remarks>
    /// Scopo: Memorizzare documenti semanticamente simili per raccomandazioni.
    /// Logica: Rimuove vecchie relazioni, salva top 5 nuove con ranking.
    /// Output: Tabella SimilarDocuments popolata per suggerimenti "Documenti correlati".
    /// </remarks>
    public async Task SaveSimilarDocumentsAsync(int sourceDocumentId, List<RelevantDocumentResult> similarDocuments)
    {
        if (similarDocuments == null || similarDocuments.Count == 0)
            return;

        // Remove any existing similar document relationships for this source document
        var existingRelationships = await _context.SimilarDocuments
            .Where(sd => sd.SourceDocumentId == sourceDocumentId)
            .ToListAsync();
        
        if (existingRelationships.Any())
        {
            _context.SimilarDocuments.RemoveRange(existingRelationships);
        }

        // Add new similar document relationships
        const int MaxSimilarDocuments = 5;
        int rank = 1;
        foreach (var similar in similarDocuments.Take(MaxSimilarDocuments))
        {
            var similarDoc = new SimilarDocument
            {
                SourceDocumentId = sourceDocumentId,
                SimilarDocumentId = similar.DocumentId,
                SimilarityScore = similar.SimilarityScore,
                RelevantChunk = similar.RelevantChunk,
                ChunkIndex = similar.ChunkIndex,
                AnalyzedAt = DateTime.UtcNow,
                Rank = rank++
            };
            
            _context.SimilarDocuments.Add(similarDoc);
        }

        await _context.SaveChangesAsync();
    }
}
