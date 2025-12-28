# Security Best Practices - DocN

## 🔐 Panoramica Sicurezza

Questo documento definisce le best practices di sicurezza per DocN, un sistema RAG documentale aziendale che gestisce dati sensibili.

## 🎯 Principi di Sicurezza

### Defense in Depth
Implementare più livelli di sicurezza:
1. **Network Layer**: Firewall, WAF, DDoS protection
2. **Application Layer**: Authentication, authorization, input validation
3. **Data Layer**: Encryption, access control, audit logging

### Principle of Least Privilege
Ogni utente, servizio, e componente deve avere solo i permessi minimi necessari.

### Zero Trust Architecture
Non fidarsi mai, verificare sempre - anche per traffico interno.

---

## 🔑 Autenticazione e Autorizzazione

### ✅ Già Implementato

- ASP.NET Core Identity con hash password bcrypt
- Role-based authorization (Admin, User)
- Cookie-based authentication
- Multi-tenancy con isolamento dati

### ❌ Da Implementare

#### 1. Multi-Factor Authentication (MFA)
```csharp
// Abilitare MFA in Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configurare authenticator app
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
});
```

**Priorità:** 🔴 Alta  
**Effort:** 1 settimana

#### 2. OAuth 2.0 / OpenID Connect
Supporto per login con:
- Microsoft Azure AD / Entra ID
- Google Workspace
- Okta / Auth0

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];
    })
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"];
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
    });
```

**Priorità:** 🟡 Media  
**Effort:** 1 settimana

#### 3. API Key Authentication
Per accesso programmatico:

```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            return AuthenticateResult.Fail("API Key missing");
        
        var user = await _userService.ValidateApiKeyAsync(apiKey);
        if (user == null)
            return AuthenticateResult.Fail("Invalid API Key");
        
        var claims = new[] { new Claim(ClaimTypes.Name, user.Email) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        return AuthenticateResult.Success(ticket);
    }
}
```

**Schema Database:**
```sql
CREATE TABLE ApiKeys (
    Id INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    KeyHash NVARCHAR(256) NOT NULL,  -- SHA256 hash
    Name NVARCHAR(100) NOT NULL,
    Scopes NVARCHAR(500) NULL,       -- JSON array: ["read:documents", "write:documents"]
    ExpiresAt DATETIME2 NULL,
    LastUsedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);
```

**Priorità:** 🔴 Alta  
**Effort:** 3-4 giorni

#### 4. Session Management
- Session timeout configurabile (default 30 minuti)
- Logout automatico dopo inattività
- Concurrent session management
- Secure cookie attributes

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
```

**Priorità:** 🟡 Media  
**Effort:** 1-2 giorni

---

## 🔒 Gestione Secrets e Credenziali

### ❌ Problemi Attuali

**CRITICO:** `appsettings.Development.json` contiene placeholder per API keys:
```json
{
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE"
  }
}
```

### ✅ Soluzioni

#### 1. User Secrets (Development)
```bash
cd DocN.Server
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "Gemini:ApiKey" "..."
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
```

#### 2. Azure Key Vault (Production)
```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

#### 3. AWS Secrets Manager (Alternative)
```csharp
builder.Configuration.AddSecretsManager(configurator: options =>
{
    options.SecretFilter = entry => entry.Name.StartsWith("DocN/");
});
```

#### 4. Environment Variables
```bash
# .env file (NON committare!)
export OPENAI_API_KEY="sk-..."
export GEMINI_API_KEY="..."
export DB_CONNECTION_STRING="..."
```

**Priorità:** 🔴 CRITICA  
**Effort:** 2-3 giorni

---

## 🛡️ Protezione Input e Validazione

### ✅ Già Implementato
- Server-side validation con DataAnnotations
- File type validation (MIME types)
- File size limits

### ❌ Da Implementare

#### 1. Input Sanitization
```csharp
public class DocumentUploadValidator : AbstractValidator<DocumentUploadRequest>
{
    public DocumentUploadValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(BeValidFileName).WithMessage("Nome file non valido");
        
        RuleFor(x => x.Category)
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9 _-]+$").WithMessage("Categoria contiene caratteri non validi");
        
        RuleFor(x => x.Notes)
            .MaximumLength(5000);
    }
    
    private bool BeValidFileName(string fileName)
    {
        // Blocca caratteri pericolosi
        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c) || c == '<' || c == '>');
    }
}
```

#### 2. XSS Protection
```csharp
public static class HtmlSanitizer
{
    private static readonly Ganss.Xss.HtmlSanitizer _sanitizer = new();
    
    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;
        
        return _sanitizer.Sanitize(html);
    }
}

// Uso
document.Notes = HtmlSanitizer.Sanitize(document.Notes);
```

#### 3. SQL Injection Protection
✅ Già protetto con Entity Framework parametrizzato, ma verificare stored procedures:

```sql
-- ❌ VULNERABILE (se costruito dinamicamente)
EXEC('SELECT * FROM Documents WHERE FileName = ''' + @FileName + '''')

-- ✅ SICURO
EXEC sp_executesql N'SELECT * FROM Documents WHERE FileName = @FileName',
    N'@FileName NVARCHAR(255)',
    @FileName
```

#### 4. Path Traversal Protection
```csharp
public class SecureFileService
{
    private readonly string _uploadBasePath;
    
    public string GetSecureFilePath(string fileName)
    {
        // Rimuovi caratteri pericolosi
        fileName = Path.GetFileName(fileName);
        
        // Costruisci path sicuro
        var fullPath = Path.Combine(_uploadBasePath, fileName);
        
        // Verifica che sia dentro basePath (previene ../)
        var normalizedPath = Path.GetFullPath(fullPath);
        var normalizedBasePath = Path.GetFullPath(_uploadBasePath);
        
        if (!normalizedPath.StartsWith(normalizedBasePath))
            throw new SecurityException("Path traversal detected");
        
        return normalizedPath;
    }
}
```

**Priorità:** 🔴 Alta  
**Effort:** 1 settimana

---

## 🔐 Encryption

### ✅ Già Implementato
- HTTPS/TLS per comunicazioni
- Password hashing con Identity

### ❌ Da Implementare

#### 1. Database Encryption at Rest

**SQL Server Transparent Data Encryption (TDE):**
```sql
-- Master key
USE master;
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'YOUR_STRONG_PASSWORD_HERE';

-- Certificate
CREATE CERTIFICATE TDECert WITH SUBJECT = 'DocN TDE Certificate';

-- Database encryption
USE DocN;
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDECert;

ALTER DATABASE DocN SET ENCRYPTION ON;
```

**Priorità:** 🟡 Media  
**Effort:** 1 giorno (configurazione)

#### 2. Application-Level Encryption

Per dati sensibili nei documenti:
```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        // IMPORTANT: Generate a new random IV for each encryption
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        
        sw.Write(plainText);
        sw.Close();
        
        // Prepend IV to ciphertext for later decryption
        var iv = aes.IV;
        var cipherText = ms.ToArray();
        var result = new byte[iv.Length + cipherText.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherText, 0, result, iv.Length, cipherText.Length);
        
        return Convert.ToBase64String(result);
    }
    
    public string Decrypt(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        
        // Extract IV from the beginning of ciphertext
        var iv = new byte[aes.IV.Length];
        var cipher = new byte[buffer.Length - iv.Length];
        Buffer.BlockCopy(buffer, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(buffer, iv.Length, cipher, 0, cipher.Length);
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
```

**Priorità:** 🟡 Media  
**Effort:** 3-4 giorni

#### 3. File Storage Encryption

**Azure Blob Storage:**
```csharp
var blobServiceClient = new BlobServiceClient(connectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("documents");

// Abilita encryption at rest (default in Azure)
// Opzionale: Customer-managed keys
var blobClient = containerClient.GetBlobClient(fileName);
await blobClient.UploadAsync(stream, new BlobUploadOptions
{
    HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
    Metadata = metadata,
    // Azure gestisce encryption automaticamente
});
```

**Priorità:** 🟡 Media  
**Effort:** 2-3 giorni

---

## 🚨 Rate Limiting e DDoS Protection

### ❌ Da Implementare

#### 1. ASP.NET Core Rate Limiting
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter per API
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
    
    // Sliding window per upload
    options.AddSlidingWindowLimiter("upload", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 10;
        opt.SegmentsPerWindow = 3;
    });
    
    // Concurrency limiter per AI operations
    options.AddConcurrencyLimiter("ai", opt =>
    {
        opt.PermitLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 20;
    });
});

// Apply to endpoints
app.MapPost("/api/v1/documents", UploadDocument)
    .RequireRateLimiting("upload");
```

#### 2. IP-based Rate Limiting
```csharp
public class IpRateLimitMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly int _maxRequests = 100;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    
    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var cacheKey = $"ratelimit_{ip}";
        
        if (_cache.TryGetValue(cacheKey, out int requestCount))
        {
            if (requestCount >= _maxRequests)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }
            _cache.Set(cacheKey, requestCount + 1, _window);
        }
        else
        {
            _cache.Set(cacheKey, 1, _window);
        }
        
        await _next(context);
    }
}
```

#### 3. WAF (Web Application Firewall)
- Azure Application Gateway WAF
- AWS WAF
- Cloudflare
- ModSecurity (open source)

**Priorità:** 🔴 Alta  
**Effort:** 1 settimana

---

## 📝 Audit Logging

### ❌ Da Implementare

```csharp
public class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }        // "DocumentUploaded", "DocumentViewed", etc.
    public string ResourceType { get; set; }  // "Document", "Configuration", etc.
    public string ResourceId { get; set; }
    public string Details { get; set; }       // JSON
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditService : IAuditService
{
    public async Task LogAsync(string action, string resourceType, string resourceId, object details)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.Id,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Details = JsonSerializer.Serialize(details),
            IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContext.Request.Headers["User-Agent"],
            Timestamp = DateTime.UtcNow
        };
        
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

// Usage
await _auditService.LogAsync(
    "DocumentUploaded",
    "Document",
    document.Id.ToString(),
    new { FileName = document.FileName, FileSize = document.FileSize }
);
```

**Eventi da Loggare:**
- ✅ Login/Logout
- ✅ Document upload/download/delete
- ✅ Document view/search
- ✅ Configuration changes
- ✅ User creation/modification
- ✅ Permission changes
- ✅ API key creation/revocation

**Priorità:** 🔴 Alta  
**Effort:** 1-2 settimane

---

## 🔍 Security Headers

### ✅ Da Implementare

```csharp
// Program.cs
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // XSS protection
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // HSTS - Force HTTPS
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://api.openai.com https://generativelanguage.googleapis.com");
    
    // Referrer Policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Permissions Policy
    context.Response.Headers.Add("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    
    await next();
});
```

**Priorità:** 🔴 Alta  
**Effort:** 1 giorno

---

## 🔒 CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(
            "https://docn.example.com",
            "https://app.docn.example.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

app.UseCors("AllowedOrigins");
```

**Priorità:** 🟡 Media  
**Effort:** 1 ora

---

## 🧪 Security Testing

### Checklist

- [ ] **OWASP Top 10 Testing**
  - [ ] Injection (SQL, NoSQL, Command)
  - [ ] Broken Authentication
  - [ ] Sensitive Data Exposure
  - [ ] XML External Entities (XXE)
  - [ ] Broken Access Control
  - [ ] Security Misconfiguration
  - [ ] Cross-Site Scripting (XSS)
  - [ ] Insecure Deserialization
  - [ ] Using Components with Known Vulnerabilities
  - [ ] Insufficient Logging & Monitoring

- [ ] **Penetration Testing**
  - [ ] Network scan (Nmap)
  - [ ] Vulnerability scan (OpenVAS, Nessus)
  - [ ] Web app scan (OWASP ZAP, Burp Suite)
  - [ ] Social engineering test

- [ ] **Automated Security Scanning**
  - [ ] SAST (Static): Roslyn analyzers, SonarQube
  - [ ] DAST (Dynamic): OWASP ZAP automation
  - [ ] Dependency scanning: Dependabot, Snyk
  - [ ] Container scanning (se Docker): Trivy, Clair

- [ ] **Code Review Checklist**
  - [ ] No hardcoded secrets
  - [ ] Proper input validation
  - [ ] Parameterized queries
  - [ ] Authentication on all endpoints
  - [ ] Authorization checks
  - [ ] Audit logging
  - [ ] Error handling (no stack trace in prod)

---

## 📋 Compliance

### GDPR (General Data Protection Regulation)

- [ ] **Right to Access**: API per export dati utente
- [ ] **Right to Erasure**: Delete account + documenti
- [ ] **Data Portability**: Export in formato machine-readable
- [ ] **Privacy by Design**: Minimizzazione dati
- [ ] **Consent Management**: Opt-in esplicito
- [ ] **Data Breach Notification**: Processo entro 72h

### ISO 27001

- [ ] Information Security Management System (ISMS)
- [ ] Risk assessment & treatment
- [ ] Security policies & procedures
- [ ] Incident response plan
- [ ] Business continuity plan

### SOC 2

- [ ] Security controls documentation
- [ ] Audit logs retention (minimo 1 anno)
- [ ] Encryption in transit & at rest
- [ ] Access control & authentication
- [ ] Monitoring & alerting
- [ ] Regular security audits

---

## 🚨 Incident Response Plan

### 1. Preparation
- [ ] Security team contacts
- [ ] Communication plan
- [ ] Forensics tools ready

### 2. Detection & Analysis
- [ ] Log monitoring
- [ ] Alerts configured
- [ ] Threat intelligence

### 3. Containment
- [ ] Isolate affected systems
- [ ] Preserve evidence
- [ ] Short-term containment

### 4. Eradication
- [ ] Remove threat
- [ ] Patch vulnerabilities
- [ ] Security hardening

### 5. Recovery
- [ ] Restore from backup
- [ ] Verify system integrity
- [ ] Monitor for re-infection

### 6. Post-Incident
- [ ] Incident report
- [ ] Lessons learned
- [ ] Update procedures

---

## ✅ Security Checklist Deployment

Prima di andare in produzione:

### Infrastructure
- [ ] Firewall configurato
- [ ] WAF abilitato
- [ ] DDoS protection attiva
- [ ] SSL/TLS certificati validi
- [ ] Database encryption abilitata
- [ ] Backup automatici configurati

### Application
- [ ] Secrets in Key Vault (non in codice)
- [ ] HTTPS enforced
- [ ] Security headers configurati
- [ ] Rate limiting attivo
- [ ] Input validation completa
- [ ] Output encoding (anti-XSS)
- [ ] CORS policy restrittiva
- [ ] API authentication robusta

### Monitoring
- [ ] Audit logging completo
- [ ] Security alerts configurati
- [ ] Log retention policy
- [ ] Incident response plan
- [ ] Security dashboard

### Compliance
- [ ] Privacy policy pubblicata
- [ ] Terms of service
- [ ] GDPR compliant
- [ ] Data processing agreement (se applicabile)
- [ ] Security audit completato

---

## 📞 Security Contacts

- **Security Team**: security@docn.example.com
- **Incident Response**: incident@docn.example.com
- **Vulnerability Disclosure**: security-disclosure@docn.example.com

---

**Documento Versione:** 1.0  
**Data:** Dicembre 2024  
**Prossima Revisione:** Trimestrale
