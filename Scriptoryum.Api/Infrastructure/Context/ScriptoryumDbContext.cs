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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

            // Relationship with ApplicationUser
            entity.HasOne(d => d.UploadedByUser)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks", "public");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.DocumentId).HasColumnName("document_id");

            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");

            entity.Property(e => e.Content).HasColumnName("content");

            // Aqui diz que a coluna é do tipo vetor com dimensão 1536
            entity.Property(e => e.Embedding)
                  .HasColumnType("vector(768)")
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
            
            entity.Property(ai => ai.DefaultProvider)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(ai => ai.OpenAIApiKey)
                .HasMaxLength(200);
            
            entity.Property(ai => ai.OpenAIModel)
                .HasMaxLength(100);
            
            entity.Property(ai => ai.ClaudeApiKey)
                .HasMaxLength(200);
            
            entity.Property(ai => ai.ClaudeModel)
                .HasMaxLength(100);
            
            entity.Property(ai => ai.GeminiApiKey)
                .HasMaxLength(200);
            
            entity.Property(ai => ai.GeminiModel)
                .HasMaxLength(100);
            
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