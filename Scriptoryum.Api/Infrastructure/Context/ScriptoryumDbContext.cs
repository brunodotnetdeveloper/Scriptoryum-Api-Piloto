using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using System.Reflection.Emit;

namespace Scriptoryum.Api.Infrastructure.Context;

public class ScriptoryumDbContext(DbContextOptions<ScriptoryumDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<ExtractedEntity> ExtractedEntities { get; set; }
    public DbSet<Insight> Insights { get; set; }
    public DbSet<RiskDetected> RisksDetected { get; set; }
    public DbSet<TimelineEvent> TimelineEvents { get; set; }
    
    // Chat System
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    
    // AI Configuration
    public DbSet<AIConfiguration> AIConfigurations { get; set; }
    public DbSet<AIProviderConfig> AIProviderConfigs { get; set; }
    
    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    
    // Service API Keys
    public DbSet<ServiceApiKey> ServiceApiKeys { get; set; }
    

    
    // Organization Management
    public DbSet<Organization> Organizations { get; set; }

    public DbSet<OrganizationAIProviderConfig> OrganizationAIProviderConfigs { get; set; }
    
    // Workspace Management
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<WorkspaceUser> WorkspaceUsers { get; set; }
    
    // Document Type Management
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<DocumentTypeField> DocumentTypeFields { get; set; }
    public DbSet<DocumentTypeTemplate> DocumentTypeTemplates { get; set; }
    public DbSet<DocumentFieldValue> DocumentFieldValues { get; set; }
    public DbSet<DocumentFieldValueHistory> DocumentFieldValueHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure DateTime to UTC conversion for PostgreSQL compatibility
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }

        // Configure snake_case naming convention
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case
            entity.SetTableName(ToSnakeCase(entity.GetTableName()));

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            // Convert key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            // Convert foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()));
            }

            // Convert index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }

        builder.HasPostgresExtension("vector"); 

        // Configure Document entity
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            
            entity.Property(d => d.ProcessedFileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(d => d.Summary);

            entity.Property(d => d.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(d=>d.FileSize)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(d => d.Description)
                .HasMaxLength(2000);
            
            entity.Property(d => d.StorageProvider)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(d => d.StoragePath)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(d => d.FileType)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(d => d.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(DocumentStatus.Uploaded);
            
            entity.Property(d => d.UploadedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(d => d.TextExtracted);

            entity.Property(d => d.ProcessingStartedAt);

            // Relationship with ApplicationUser
            entity.HasOne(d => d.UploadedByUser)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with Workspace
            entity.HasOne(d => d.Workspace)
                .WithMany(w => w.Documents)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with DocumentType
            entity.HasOne(d => d.DocumentType)
                .WithMany(dt => dt.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            // Index for better query performance
            entity.HasIndex(d => d.WorkspaceId);
            entity.HasIndex(d => d.DocumentTypeId);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.UploadedAt);
        });

        builder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks", "public");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.DocumentId).HasColumnName("document_id");

            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");

            entity.Property(e => e.Content).HasColumnName("content");

            // Aqui diz que a coluna � do tipo vetor com dimens�o 1536
            entity.Property(e => e.Embedding)
                  .HasColumnType("vector(1536)")
                  .HasColumnName("embedding");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Foreign key para Document (ajuste conforme seu modelo)
            entity.HasOne(e => e.Document)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId);

            // Opcional: index para busca vetorial (ivfflat)
            entity.HasIndex(e => e.Embedding)
                  .HasMethod("ivfflat")
                  .HasOperators("vector_l2_ops");
        });


        // Configure ExtractedEntity
        builder.Entity<ExtractedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.ConfidenceScore)
                .HasPrecision(5, 4);
            
            entity.Property(e => e.ContextExcerpt)
                .HasMaxLength(1000);

            // Relationship with Document
            entity.HasOne(e => e.Document)
                .WithMany(d => d.ExtractedEntities)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Insight
        builder.Entity<Insight>(entity =>
        {
            entity.HasKey(i => i.Id);
            
            entity.Property(i => i.Category)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(i => i.Description)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(i => i.ImportanceLevel)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(i => i.ExtractedText)
                .HasMaxLength(2000);

            // Relationship with Document
            entity.HasOne(i => i.Document)
                .WithMany(d => d.Insights)
                .HasForeignKey(i => i.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RiskDetected
        builder.Entity<RiskDetected>(entity =>
        {
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(r => r.RiskLevel)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(r => r.ConfidenceScore)
                .HasPrecision(5, 4);
            
            entity.Property(r => r.EvidenceExcerpt)
                .HasMaxLength(2000);
            
            entity.Property(r => r.DetectedAt)
                .HasDefaultValueSql("NOW()");

            // Relationship with Document
            entity.HasOne(r => r.Document)
                .WithMany(d => d.RisksDetected)
                .HasForeignKey(r => r.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TimelineEvent
        builder.Entity<TimelineEvent>(entity =>
        {
            entity.HasKey(t => t.Id);
            
            entity.Property(t => t.EventType)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(t => t.SourceExcerpt)
                .HasMaxLength(2000);

            // Relationship with Document
            entity.HasOne(t => t.Document)
                .WithMany(d => d.TimelineEvents)
                .HasForeignKey(t => t.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChatSession
        builder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(cs => cs.Id);
            
            entity.Property(cs => cs.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(cs => cs.Description)
                .HasMaxLength(500);
            
            entity.Property(cs => cs.MessageCount)
                .HasDefaultValue(0);
            
            entity.Property(cs => cs.LastActivityAt)
                .HasDefaultValueSql("NOW()");

            // Relationship with ApplicationUser
            entity.HasOne(cs => cs.User)
                .WithMany(u => u.ChatSessions)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Document (optional)
            entity.HasOne(cs => cs.Document)
                .WithMany()
                .HasForeignKey(cs => cs.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ChatMessage
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.Id);
            
            entity.Property(cm => cm.Role)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(cm => cm.Content)
                .IsRequired();
            
            entity.Property(cm => cm.DocumentName)
                .HasMaxLength(500);
            
            entity.Property(cm => cm.Cost)
                .HasPrecision(10, 6);
            
            entity.Property(cm => cm.AIProvider)
                .HasConversion<string>();
            
            entity.Property(cm => cm.ModelUsed)
                .HasMaxLength(100);

            // Relationship with ChatSession
            entity.HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Document (optional)
            entity.HasOne(cm => cm.Document)
                .WithMany()
                .HasForeignKey(cm => cm.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AIConfiguration
        builder.Entity<AIConfiguration>(entity =>
        {
            entity.HasKey(ai => ai.Id);
            
            entity.Property(ai => ai.MaxTokens)
                .HasDefaultValue(4000);
            
            entity.Property(ai => ai.Temperature)
                .HasPrecision(3, 2)
                .HasDefaultValue(0.7m);

            // Relationship with ApplicationUser (one-to-one)
            entity.HasOne(ai => ai.User)
                .WithOne(u => u.AIConfiguration)
                .HasForeignKey<AIConfiguration>(ai => ai.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AIProviderConfig
        builder.Entity<AIProviderConfig>(entity =>
        {
            entity.HasKey(apc => apc.Id);
            
            entity.Property(apc => apc.Provider)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(apc => apc.ApiKey)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(apc => apc.SelectedModel)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(apc => apc.IsEnabled)
                .HasDefaultValue(true);
            
            entity.Property(apc => apc.LastTestMessage)
                .HasMaxLength(500);

            // Relationship with AIConfiguration (many-to-one)
            entity.HasOne(apc => apc.AIConfiguration)
                .WithMany(ai => ai.AIProviderConfigs)
                .HasForeignKey(apc => apc.AIConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Notification
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            
            entity.Property(n => n.Type)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(n => n.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(NotificationStatus.Unread);
            
            entity.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(n => n.AdditionalData)
                .HasMaxLength(2000);

            // Relationship with ApplicationUser
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Document (optional)
            entity.HasOne(n => n.Document)
                .WithMany()
                .HasForeignKey(n => n.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Index for better query performance
            entity.HasIndex(n => new { n.UserId, n.Status });
            entity.HasIndex(n => n.CreatedAt);
        });

        // Configure ServiceApiKey
        builder.Entity<ServiceApiKey>(entity =>
        {
            entity.HasKey(sak => sak.Id);
            
            entity.Property(sak => sak.ServiceName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(sak => sak.Description)
                .HasMaxLength(500);
            
            entity.Property(sak => sak.ApiKeyHash)
                .IsRequired()
                .HasMaxLength(128); // SHA-256 hash
            
            entity.Property(sak => sak.KeyPrefix)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(sak => sak.KeySuffix)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(sak => sak.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(ServiceApiKeyStatus.Active);
            
            entity.Property(sak => sak.UsageCount)
                .HasDefaultValue(0);
            
            entity.Property(sak => sak.CurrentMonthUsage)
                .HasDefaultValue(0);
            
            entity.Property(sak => sak.CurrentMonthYear)
                .IsRequired()
                .HasMaxLength(7); // YYYY-MM format
            
            entity.Property(sak => sak.Permissions)
                .HasMaxLength(2000);
            
            entity.Property(sak => sak.AllowedIPs)
                .HasMaxLength(1000);

            // Relationship with ApplicationUser
            entity.HasOne(sak => sak.CreatedByUser)
                .WithMany()
                .HasForeignKey(sak => sak.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with Organization
            entity.HasOne(sak => sak.Organization)
                .WithMany()
                .HasForeignKey(sak => sak.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with Workspace (optional)
            entity.HasOne(sak => sak.Workspace)
                .WithMany(w => w.ServiceApiKeys)
                .HasForeignKey(sak => sak.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            // Indexes for better query performance
            entity.HasIndex(sak => sak.ApiKeyHash)
                .IsUnique();
            entity.HasIndex(sak => sak.Status);
            entity.HasIndex(sak => sak.CreatedByUserId);
            entity.HasIndex(sak => sak.OrganizationId);
            entity.HasIndex(sak => sak.WorkspaceId);
            entity.HasIndex(sak => sak.ExpiresAt);
        });

        // Configure Organization entity
        builder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);
            
            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(o => o.Cnpj)
                .HasMaxLength(18);
            
            entity.Property(o => o.ContactEmail)
                .IsRequired()
                .HasMaxLength(256);
            
            entity.Property(o => o.ContactPhone)
                .HasMaxLength(20);
            
            entity.Property(o => o.Address)
                .HasMaxLength(500);
            
            entity.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(OrganizationStatus.Active);
        });

        // Configure ApplicationUser entity
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(OrganizationRole.Member);
            
            entity.Property(u => u.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(OrganizationUserStatus.Active);
            
            // Relationship with Organization
            entity.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            
            // Index for organization-based queries
            entity.HasIndex(u => u.OrganizationId);
        });

        // Configure Workspace entity
        builder.Entity<Workspace>(entity =>
        {
            entity.HasKey(w => w.Id);
            
            entity.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(w => w.Description)
                .HasMaxLength(1000);
            
            entity.Property(w => w.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(WorkspaceStatus.Active);
            
            // Relationship with Organization
            entity.HasOne(w => w.Organization)
                .WithMany(o => o.Workspaces)
                .HasForeignKey(w => w.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for organization-based queries
            entity.HasIndex(w => w.OrganizationId);
        });



        // Configure WorkspaceUser entity
        builder.Entity<WorkspaceUser>(entity =>
        {
            entity.HasKey(wu => wu.Id);
            
            entity.Property(wu => wu.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(WorkspaceRole.Member);
            
            entity.Property(wu => wu.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(WorkspaceUserStatus.Active);
            
            // Relationships
            entity.HasOne(wu => wu.Workspace)
                .WithMany(w => w.WorkspaceUsers)
                .HasForeignKey(wu => wu.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(wu => wu.User)
                .WithMany(u => u.WorkspaceUsers)
                .HasForeignKey(wu => wu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: one user per workspace
            entity.HasIndex(wu => new { wu.WorkspaceId, wu.UserId })
                .IsUnique();
        });

        // Configure OrganizationAIProviderConfig entity
        builder.Entity<OrganizationAIProviderConfig>(entity =>
        {
            entity.HasKey(oapc => oapc.Id);
            
            entity.Property(oapc => oapc.Provider)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(oapc => oapc.ApiKey)
                .IsRequired()
                .HasMaxLength(500); // Encrypted key can be longer
            
            entity.Property(oapc => oapc.SelectedModel)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(oapc => oapc.IsEnabled)
                .HasDefaultValue(true);
            
            entity.Property(oapc => oapc.LastTestMessage)
                .HasMaxLength(1000);
            
            entity.Property(oapc => oapc.MonthlyTokenLimit)
                .HasDefaultValue(null);
            
            entity.Property(oapc => oapc.TokensUsedThisMonth)
                .HasDefaultValue(0);
            
            // Relationship with Organization
            entity.HasOne(oapc => oapc.Organization)
                .WithMany(o => o.AIProviderConfigs)
                .HasForeignKey(oapc => oapc.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for organization-based queries
            entity.HasIndex(oapc => oapc.OrganizationId);
        });

        // Configure DocumentType entity
        builder.Entity<DocumentType>(entity =>
        {
            entity.HasKey(dt => dt.Id);
            
            entity.Property(dt => dt.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(dt => dt.Description)
                .HasMaxLength(500);
            
            entity.Property(dt => dt.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            
            entity.Property(dt => dt.IsSystemDefault)
                .HasDefaultValue(false);
            
            entity.Property(dt => dt.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.Property(dt => dt.UpdatedAt)
                .HasDefaultValueSql("NOW()");
            
            // Relationship with Organization
            entity.HasOne(dt => dt.Organization)
                .WithMany()
                .HasForeignKey(dt => dt.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Index for better query performance
            entity.HasIndex(dt => new { dt.OrganizationId, dt.Name })
                .IsUnique();
            entity.HasIndex(dt => dt.Status);
        });

        // Configure DocumentTypeField entity
        builder.Entity<DocumentTypeField>(entity =>
        {
            entity.HasKey(dtf => dtf.Id);
            
            entity.Property(dtf => dtf.FieldName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(dtf => dtf.FieldType)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(dtf => dtf.Description)
                .HasMaxLength(500);
            
            entity.Property(dtf => dtf.ExtractionPrompt)
                .HasColumnType("text");
            
            entity.Property(dtf => dtf.IsRequired)
                .HasDefaultValue(false);
            
            entity.Property(dtf => dtf.ValidationRegex)
                .HasMaxLength(500);
            
            entity.Property(dtf => dtf.DefaultValue)
                .HasMaxLength(500);
            
            entity.Property(dtf => dtf.FieldOrder)
                .HasDefaultValue(1);
            
            entity.Property(dtf => dtf.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            
            entity.Property(dtf => dtf.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.Property(dtf => dtf.UpdatedAt)
                .HasDefaultValueSql("NOW()");
            
            // Relationship with DocumentType
            entity.HasOne(dtf => dtf.DocumentType)
                .WithMany(dt => dt.Fields)
                .HasForeignKey(dtf => dtf.DocumentTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for better query performance
            entity.HasIndex(dtf => new { dtf.DocumentTypeId, dtf.FieldName })
                .IsUnique();
            entity.HasIndex(dtf => dtf.FieldOrder);
        });

        // Configure DocumentTypeTemplate entity
        builder.Entity<DocumentTypeTemplate>(entity =>
        {
            entity.HasKey(dtt => dtt.Id);
            
            entity.Property(dtt => dtt.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(dtt => dtt.Description)
                .HasMaxLength(500);
            
            entity.Property(dtt => dtt.Category)
                .HasMaxLength(50);
            
            entity.Property(dtt => dtt.TemplateData)
                .IsRequired()
                .HasColumnType("jsonb");
            
            entity.Property(dtt => dtt.IsPublic)
                .HasDefaultValue(true);
            
            entity.Property(dtt => dtt.UsageCount)
                .HasDefaultValue(0);
            
            entity.Property(dtt => dtt.CreatedByUserId)
                .HasMaxLength(450);
            
            entity.Property(dtt => dtt.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.Property(dtt => dtt.UpdatedAt)
                .HasDefaultValueSql("NOW()");
            
            // Relationship with Organization (required)
            entity.HasOne(dtt => dtt.Organization)
                .WithMany()
                .HasForeignKey(dtt => dtt.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with ApplicationUser (optional)
            entity.HasOne(dtt => dtt.CreatedByUser)
                .WithMany()
                .HasForeignKey(dtt => dtt.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            
            // Index for better query performance
            entity.HasIndex(dtt => dtt.Category);
            entity.HasIndex(dtt => dtt.IsPublic);
            entity.HasIndex(dtt => dtt.OrganizationId);
            entity.HasIndex(dtt => dtt.UsageCount);
        });

        // Configure DocumentFieldValue entity
        builder.Entity<DocumentFieldValue>(entity =>
        {
            entity.HasKey(dfv => dfv.Id);
            
            entity.Property(dfv => dfv.ExtractedValue)
                .HasColumnType("text");
            
            entity.Property(dfv => dfv.ConfidenceScore)
                .HasPrecision(5, 4)
                .HasDefaultValue(0.0m);
            
            entity.Property(dfv => dfv.ValidationStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            
            entity.Property(dfv => dfv.ContextExcerpt)
                .HasMaxLength(2000);
            
            entity.Property(dfv => dfv.CorrectedValue)
                .HasColumnType("text");
            
            entity.Property(dfv => dfv.ExtractionMetadata)
                .HasColumnType("jsonb");
            
            entity.Property(dfv => dfv.ValidatedAt)
                .IsRequired(false);
            
            entity.Property(dfv => dfv.ValidatedByUserId)
                .HasMaxLength(450)
                .IsRequired(false);
            
            entity.Property(dfv => dfv.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.Property(dfv => dfv.UpdatedAt)
                .HasDefaultValueSql("NOW()");
            
            // Relationship with Document
            entity.HasOne(dfv => dfv.Document)
                .WithMany(d => d.FieldValues)
                .HasForeignKey(dfv => dfv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with DocumentTypeField
            entity.HasOne(dfv => dfv.DocumentTypeField)
                .WithMany(dtf => dtf.FieldValues)
                .HasForeignKey(dfv => dfv.DocumentTypeFieldId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with ApplicationUser (validator)
            entity.HasOne(dfv => dfv.ValidatedByUser)
                .WithMany()
                .HasForeignKey(dfv => dfv.ValidatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            
            // Index for better query performance
            entity.HasIndex(dfv => new { dfv.DocumentId, dfv.DocumentTypeFieldId })
                .IsUnique();
            entity.HasIndex(dfv => dfv.ValidationStatus);
            entity.HasIndex(dfv => dfv.ValidatedAt);
        });

        // Configure DocumentFieldValueHistory entity
        builder.Entity<DocumentFieldValueHistory>(entity =>
        {
            entity.HasKey(dfvh => dfvh.Id);
            
            entity.Property(dfvh => dfvh.PreviousValue)
                .HasColumnType("text");
            
            entity.Property(dfvh => dfvh.NewValue)
                .HasColumnType("text");
            
            entity.Property(dfvh => dfvh.ChangeReason)
                .HasMaxLength(500);
            
            entity.Property(dfvh => dfvh.ChangedByUserId)
                .HasMaxLength(450)
                .IsRequired(false);
                        
            entity.Property(dfvh => dfvh.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.Property(dfvh => dfvh.UpdatedAt)
                .HasDefaultValueSql("NOW()");
            
            // Relationship with DocumentFieldValue
            entity.HasOne(dfvh => dfvh.DocumentFieldValue)
                .WithMany(dfv => dfv.History)
                .HasForeignKey(dfvh => dfvh.DocumentFieldValueId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with ApplicationUser (who made the change)
            entity.HasOne(dfvh => dfvh.ChangedByUser)
                .WithMany()
                .HasForeignKey(dfvh => dfvh.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            
            // Index for better query performance
            entity.HasIndex(dfvh => dfvh.DocumentFieldValueId);
            entity.HasIndex(dfvh => dfvh.UpdatedAt);
            entity.HasIndex(dfvh => dfvh.ChangedByUserId);
        });

        // Configure EntityBase properties for all entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .Property("CreatedAt")
                    .HasDefaultValueSql("NOW()");
                
                builder.Entity(entityType.ClrType)
                    .Property("UpdatedAt")
                    .HasDefaultValueSql("NOW()");
            }
        }
        
        // Add organizational integrity constraint
        // Ensures that a document can only use document types from the same organization as its workspace
        builder.Entity<Document>()
            .ToTable(tb => tb.HasCheckConstraint(
                "CK_Document_OrganizationalIntegrity",
                "document_type_id IS NULL OR EXISTS (" +
                "SELECT 1 FROM workspaces w " +
                "INNER JOIN document_types dt ON dt.organization_id = w.organization_id " +
                "WHERE w.id = workspace_id AND dt.id = document_type_id)"));
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            if (char.IsUpper(c))
            {
                if (i > 0 && input[i - 1] != '_')
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }
}