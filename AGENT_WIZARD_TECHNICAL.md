# Agent Wizard - Technical Implementation Documentation

## Architecture Overview

The Agent Wizard system provides a guided interface for creating and managing RAG (Retrieval-Augmented Generation) agents without requiring technical knowledge. The implementation follows a clean architecture pattern with clear separation between UI, business logic, and data layers.

## Component Structure

```
DocN/
├── DocN.Data/
│   ├── Models/
│   │   ├── AgentConfiguration.cs          # Agent configuration entity
│   │   ├── AgentTemplate.cs                # Predefined agent templates
│   │   └── AgentUsageLog.cs                # Usage tracking and analytics
│   ├── Services/
│   │   ├── AgentConfigurationService.cs    # CRUD operations for agents
│   │   └── AgentTemplateSeeder.cs          # Seeds predefined templates
│   └── ApplicationDbContext.cs             # EF Core context with agent tables
├── DocN.Server/
│   └── Controllers/
│       └── AgentController.cs              # REST API endpoints
├── DocN.Client/
│   └── Components/Pages/
│       ├── AgentWizard.razor               # Main wizard component
│       ├── AgentWizard/
│       │   ├── Step1_ChooseTemplate.razor  # Template selection
│       │   ├── Step2_ConfigureProvider.razor # Provider & API key setup
│       │   ├── Step3_Customize.razor       # Parameter configuration
│       │   ├── Step4_Test.razor            # Testing and validation
│       │   └── Step5_Complete.razor        # Summary and save
│       └── Agents.razor                    # Agent management page
└── Database/
    └── Migrations/
        └── 20251229_AddAgentConfigurationTables.sql  # DB migration
```

## Data Models

### AgentConfiguration

Represents a configured AI agent for RAG operations.

```csharp
public class AgentConfiguration
{
    public int Id { get; set; }
    
    // Basic Information
    public string Name { get; set; }
    public string Description { get; set; }
    public AgentType AgentType { get; set; }
    
    // Provider Configuration
    public AIProviderType PrimaryProvider { get; set; }
    public AIProviderType? FallbackProvider { get; set; }
    public string? ModelName { get; set; }
    public string? EmbeddingModelName { get; set; }
    
    // RAG Parameters
    public int MaxDocumentsToRetrieve { get; set; } = 5;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxTokensForContext { get; set; } = 4000;
    public int MaxTokensForResponse { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    
    // System Instructions
    public string SystemPrompt { get; set; }
    public string? CustomInstructions { get; set; }
    
    // Capabilities
    public bool CanRetrieveDocuments { get; set; } = true;
    public bool CanClassifyDocuments { get; set; } = false;
    public bool CanExtractTags { get; set; } = false;
    public bool CanSummarize { get; set; } = true;
    public bool CanAnswer { get; set; } = true;
    
    // Search Configuration
    public bool UseHybridSearch { get; set; } = true;
    public double HybridSearchAlpha { get; set; } = 0.5;
    
    // Advanced Options
    public bool EnableConversationHistory { get; set; } = true;
    public int MaxConversationHistoryMessages { get; set; } = 10;
    public bool EnableCitation { get; set; } = true;
    public bool EnableStreaming { get; set; } = false;
    
    // Filters
    public string? CategoryFilter { get; set; }  // JSON array
    public string? TagFilter { get; set; }  // JSON array
    public DocumentVisibility? VisibilityFilter { get; set; }
    
    // Metadata
    public bool IsActive { get; set; } = true;
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; } = 0;
    
    // Relationships
    public string? OwnerId { get; set; }
    public int? TenantId { get; set; }
    public int? TemplateId { get; set; }
}
```

### AgentTemplate

Predefined templates for quick agent creation.

```csharp
public class AgentTemplate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; } = "🤖";
    public AgentType AgentType { get; set; }
    public string Category { get; set; } = "General";
    
    public AIProviderType RecommendedProvider { get; set; }
    public string? RecommendedModel { get; set; }
    public string DefaultSystemPrompt { get; set; }
    public string DefaultParametersJson { get; set; } = "{}";
    
    public bool IsBuiltIn { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    
    public string? ExampleQuery { get; set; }
    public string? ExampleResponse { get; set; }
    public string? ConfigurationGuide { get; set; }
}
```

### AgentUsageLog

Tracks agent usage for analytics and monitoring.

```csharp
public class AgentUsageLog
{
    public int Id { get; set; }
    public int AgentConfigurationId { get; set; }
    
    public string Query { get; set; }
    public string? Response { get; set; }
    
    // Performance Metrics (stored as Ticks for SQL compatibility)
    public long RetrievalTimeTicks { get; set; }
    public long SynthesisTimeTicks { get; set; }
    public long TotalTimeTicks { get; set; }
    public int DocumentsRetrieved { get; set; }
    
    // Token Usage
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    
    // Provider and Model
    public AIProviderType ProviderUsed { get; set; }
    public string? ModelUsed { get; set; }
    
    // Quality Metrics
    public double? RelevanceScore { get; set; }
    public bool? UserFeedbackPositive { get; set; }
    public string? UserFeedbackComment { get; set; }
    
    // Error Tracking
    public bool IsError { get; set; } = false;
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

## API Endpoints

### AgentController

```csharp
// Get all agent templates
GET /api/agent/templates
Response: List<AgentTemplate>

// Get specific template
GET /api/agent/templates/{id}
Response: AgentTemplate

// Get user's agents
GET /api/agent/my-agents
Response: List<AgentConfiguration>

// Get public agents
GET /api/agent/public
Response: List<AgentConfiguration>

// Get specific agent
GET /api/agent/{id}
Response: AgentConfiguration

// Create agent from template
POST /api/agent/create-from-template/{templateId}
Response: AgentConfiguration

// Create custom agent
POST /api/agent
Body: AgentConfiguration
Response: AgentConfiguration

// Update agent
PUT /api/agent/{id}
Body: AgentConfiguration
Response: AgentConfiguration

// Delete agent (soft delete)
DELETE /api/agent/{id}
Response: 204 No Content

// Test agent
POST /api/agent/{id}/test
Body: { testQuery: string }
Response: { isValid: bool, message: string }

// Get usage statistics
GET /api/agent/{id}/statistics?from={date}&to={date}
Response: List<AgentUsageLog>
```

## Service Layer

### IAgentConfigurationService

```csharp
public interface IAgentConfigurationService
{
    Task<List<AgentConfiguration>> GetAgentsByUserAsync(string userId, int? tenantId = null);
    Task<List<AgentConfiguration>> GetPublicAgentsAsync(int? tenantId = null);
    Task<AgentConfiguration?> GetAgentByIdAsync(int agentId);
    Task<AgentConfiguration> CreateAgentAsync(AgentConfiguration agent);
    Task<AgentConfiguration> UpdateAgentAsync(AgentConfiguration agent);
    Task<bool> DeleteAgentAsync(int agentId);
    Task<AgentConfiguration> CreateAgentFromTemplateAsync(int templateId, string userId, int? tenantId = null);
    Task<bool> TestAgentAsync(int agentId, string testQuery);
    Task<List<AgentTemplate>> GetTemplatesAsync();
    Task<AgentTemplate?> GetTemplateByIdAsync(int templateId);
    Task LogAgentUsageAsync(AgentUsageLog log);
    Task<List<AgentUsageLog>> GetAgentUsageStatisticsAsync(int agentId, DateTime? from = null, DateTime? to = null);
}
```

## Wizard Flow

### Step 1: Choose Template
- Display all available templates in a grid
- Show template icon, name, description, category
- Provide example queries and responses
- User selects a template

### Step 2: Configure Provider
- User selects AI provider (Gemini, OpenAI, Azure OpenAI)
- Display step-by-step instructions for obtaining API keys
- Provide direct links to provider platforms
- Highlight free options (Gemini, OpenAI trial)
- User enters API credentials

### Step 3: Customize Parameters
- Pre-filled with template defaults
- User can modify:
  - Agent name and description
  - Search parameters (documents, threshold, hybrid search)
  - Generation parameters (temperature, tokens, citations)
  - Advanced options (custom instructions, filters)
- Collapsible advanced section to avoid overwhelming users

### Step 4: Test Configuration
- Display configuration summary
- Allow user to test with a sample query
- Validate configuration (not full RAG test)
- Show success/error feedback

### Step 5: Complete
- Show comprehensive summary
- Display "Next Steps" guide
- Provide tips for using the agent
- Save button navigates to agent list

## Database Migration

### Migration Script Location
```
Database/Migrations/20251229_AddAgentConfigurationTables.sql
```

### Tables Created
1. **AgentTemplates**: Stores predefined templates
2. **AgentConfigurations**: Stores user-configured agents
3. **AgentUsageLogs**: Tracks usage and performance

### Indexes
- Performance indexes on OwnerId, TenantId, AgentType, IsActive
- Analytics indexes on CreatedAt, AgentConfigurationId
- Category and Type indexes for filtering

### Migration is Idempotent
- Safe to run multiple times
- Checks for table existence before creating
- Uses proper foreign key constraints with CASCADE/SET NULL

## Security Considerations

### API Key Storage
- **Current**: Stored in configuration models
- **TODO**: Implement encryption at rest
- **TODO**: Use Azure Key Vault or similar for production

### Authorization
- Users can only access their own agents (unless public)
- Public agents are read-only for non-owners
- Tenant isolation enforced

### Input Validation
- Agent names: max 200 characters
- Temperature: 0-1 range
- Token limits: reasonable ranges
- JSON fields validated before parsing

## Integration Points

### Existing RAG System Integration
- Agents use existing ISemanticRAGService
- Leverage existing document retrieval
- Use existing embedding service
- Compatible with current vector search

### Future Integration Points
- Chat interface can use specific agent configurations
- Document classification can use classification agents
- Batch processing can leverage agent capabilities

## Performance Considerations

### Caching
- Template list cached in memory
- Agent configurations cached per user session
- Usage logs written asynchronously

### Database Optimization
- Indexes on frequently queried fields
- Soft delete for agents (preserves history)
- Pagination support for large result sets

### API Optimization
- Minimal data transfer (only required fields)
- Eager loading for related entities
- Async/await throughout

## Testing Strategy

### Unit Tests
- AgentConfigurationService CRUD operations
- Template seeder logic
- Model validation
- API key validation

### Integration Tests
- End-to-end wizard flow
- Agent creation and modification
- Template application
- API endpoint testing

### Manual Testing Checklist
- [ ] Complete wizard flow from start to finish
- [ ] Create agent from each template type
- [ ] Test with all three providers
- [ ] Modify existing agent
- [ ] Delete agent
- [ ] Test public vs private agents
- [ ] Verify tenant isolation
- [ ] Test API key validation

## Deployment Checklist

1. **Database**
   - [ ] Backup existing database
   - [ ] Run migration script
   - [ ] Verify tables created
   - [ ] Run template seeder

2. **Backend**
   - [ ] Deploy updated DocN.Server
   - [ ] Verify services registered
   - [ ] Test API endpoints
   - [ ] Check template seeding

3. **Frontend**
   - [ ] Deploy updated DocN.Client
   - [ ] Verify wizard accessible
   - [ ] Test navigation menu
   - [ ] Verify responsive design

4. **Configuration**
   - [ ] Update appsettings for AI providers
   - [ ] Configure CORS if needed
   - [ ] Set up logging

5. **Documentation**
   - [ ] Update user documentation
   - [ ] Create video tutorial (optional)
   - [ ] Update API documentation

## Monitoring and Analytics

### Metrics to Track
- Number of agents created per day/week
- Most popular templates
- Provider distribution (Gemini vs OpenAI vs Azure)
- Agent usage frequency
- Average response time
- Error rates
- User satisfaction (feedback)

### Logging
- Agent creation events
- Configuration changes
- Usage patterns
- Errors and exceptions

## Future Enhancements

### Phase 2 Features
- [ ] Agent performance dashboard
- [ ] A/B testing between configurations
- [ ] Automatic parameter tuning based on usage
- [ ] Agent sharing and collaboration
- [ ] Export/import agent configurations
- [ ] Custom template creation by users
- [ ] Multi-language support
- [ ] Voice interaction with agents
- [ ] Agent marketplace (community templates)

### Technical Improvements
- [ ] Encrypt API keys at rest
- [ ] Implement key rotation
- [ ] Add rate limiting per agent
- [ ] Streaming responses support
- [ ] Fine-tuning integration
- [ ] Cost tracking per agent
- [ ] Advanced analytics dashboard

## Troubleshooting

### Common Issues

**Agent doesn't retrieve documents**
- Check document upload status
- Verify embedding generation completed
- Check similarity threshold (lower if no results)

**API key validation fails**
- Verify key format
- Check provider service status
- Ensure key has correct permissions

**Template not appearing**
- Run template seeder manually
- Check IsActive flag in database
- Verify database migration completed

**Performance issues**
- Check number of documents being retrieved
- Verify embedding dimensions match
- Check database indexes exist
- Monitor token usage

## Support and Maintenance

### Regular Maintenance Tasks
- Monitor agent usage logs
- Clean up old test agents
- Update template defaults based on feedback
- Review and optimize popular agents
- Update provider configurations

### Version History
- **v1.0** (2025-12-29): Initial release
  - 8 predefined templates
  - 3 provider support
  - 5-step wizard
  - Basic analytics

---

**Document Version**: 1.0  
**Last Updated**: December 29, 2025  
**Author**: AI Agent Development Team
