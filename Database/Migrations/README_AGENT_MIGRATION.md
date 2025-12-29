# Agent Configuration System - Database Migration

## Overview
This migration adds support for AI Agent configuration and management to the DocN system.

## Tables Added

### 1. AgentTemplates
Stores predefined agent templates for quick configuration:
- Question & Answer agents
- Document summarization agents
- Legal document analysis agents
- HR documentation agents
- Technical documentation agents
- Financial document agents
- Document comparison agents
- Custom agents

### 2. AgentConfigurations
Stores user-configured agents with their specific settings:
- Provider configuration (Gemini, OpenAI, Azure OpenAI)
- RAG parameters (retrieval count, similarity threshold, etc.)
- System prompts and instructions
- Capabilities and permissions
- Search configuration
- Filters and scope

### 3. AgentUsageLogs
Tracks agent usage for analytics and monitoring:
- Query and response logging
- Performance metrics (retrieval time, synthesis time)
- Token usage tracking
- Quality metrics and user feedback
- Error tracking

## How to Apply Migration

### Using SQL Server Management Studio (SSMS)
1. Open SQL Server Management Studio
2. Connect to your DocN database
3. Open the migration file: `20251229_AddAgentConfigurationTables.sql`
4. Execute the script (F5 or Execute button)
5. Verify tables were created successfully in the Messages tab

### Using Azure Data Studio
1. Open Azure Data Studio
2. Connect to your DocN database
3. Open the migration file: `20251229_AddAgentConfigurationTables.sql`
4. Click "Run" button
5. Check the results panel for success messages

### Using sqlcmd Command Line
```bash
sqlcmd -S your_server_name -d DocN -i 20251229_AddAgentConfigurationTables.sql
```

### Using Entity Framework Core (Alternative)
If you prefer using EF Core migrations:
```bash
cd DocN.Data
dotnet ef migrations add AddAgentConfigurationTables
dotnet ef database update
```

## Seeding Agent Templates

After applying the migration, you need to seed the default agent templates. This can be done by:

1. **Automatic seeding on application startup** (recommended):
   The `AgentTemplateSeeder` will run automatically when the application starts

2. **Manual seeding via API** (if needed):
   Call the seed endpoint (if implemented) or run the seeder manually

## Rollback (if needed)

If you need to rollback this migration:

```sql
-- Drop tables in reverse order (respecting foreign keys)
DROP TABLE IF EXISTS AgentUsageLogs;
DROP TABLE IF EXISTS AgentConfigurations;
DROP TABLE IF EXISTS AgentTemplates;
```

## Verification

After running the migration, verify the tables exist:

```sql
-- Check if tables were created
SELECT name FROM sys.tables 
WHERE name IN ('AgentTemplates', 'AgentConfigurations', 'AgentUsageLogs')
ORDER BY name;

-- Check table structure
EXEC sp_help 'AgentTemplates';
EXEC sp_help 'AgentConfigurations';
EXEC sp_help 'AgentUsageLogs';
```

## Notes

- The migration script is idempotent (safe to run multiple times)
- All foreign keys include appropriate CASCADE/SET NULL rules
- Indexes are added for query performance
- TimeSpan values are stored as BIGINT ticks for SQL compatibility

## Support

For issues or questions about this migration, please refer to:
- Database documentation in `/Database/README.md`
- Agent configuration guide (to be added)
