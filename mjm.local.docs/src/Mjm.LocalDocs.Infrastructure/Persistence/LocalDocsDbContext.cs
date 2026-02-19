using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Infrastructure.Persistence.Entities;

namespace Mjm.LocalDocs.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for LocalDocs.
/// Embeddings are stored separately (sqlite-vec for SQLite, chunk_embeddings table for SQL Server).
/// </summary>
public sealed class LocalDocsDbContext : DbContext
{
    public LocalDocsDbContext(DbContextOptions<LocalDocsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<DocumentChunkEntity> DocumentChunks => Set<DocumentChunkEntity>();
    public DbSet<ApiTokenEntity> ApiTokens => Set<ApiTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProject(modelBuilder);
        ConfigureDocument(modelBuilder);
        ConfigureDocumentChunk(modelBuilder);
        ConfigureApiToken(modelBuilder);
    }

    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectEntity>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Cascade delete: when project is deleted, delete all documents
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Project)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.FileExtension)
                .HasMaxLength(20)
                .IsRequired();

            // FileContent is nullable - content may be stored externally
            entity.Property(e => e.FileContent);

            // FileStorageLocation stores the path/URI for externally stored files
            entity.Property(e => e.FileStorageLocation)
                .HasMaxLength(1000);

            entity.Property(e => e.FileSizeBytes)
                .IsRequired();

            entity.Property(e => e.ExtractedText)
                .IsRequired();

            entity.Property(e => e.ContentHash)
                .HasMaxLength(64); // SHA256 hex string

            entity.Property(e => e.MetadataJson);

            entity.Property(e => e.VersionNumber)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.ParentDocumentId)
                .HasMaxLength(36);

            entity.Property(e => e.IsSuperseded)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.ParentDocumentId);
            entity.HasIndex(e => e.IsSuperseded);
            entity.HasIndex(e => new { e.ProjectId, e.FileName });

            // Cascade delete: when document is deleted, delete all chunks
            entity.HasMany(e => e.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDocumentChunk(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentChunkEntity>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(100) // {documentId}_chunk_{index}
                .IsRequired();

            entity.Property(e => e.DocumentId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.FileName)
                .HasMaxLength(500);

            entity.Property(e => e.ChunkIndex)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
        });
    }

    private static void ConfigureApiToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiTokenEntity>(entity =>
        {
            entity.ToTable("ApiTokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.TokenHash)
                .HasMaxLength(64) // Base64-encoded SHA256
                .IsRequired();

            entity.Property(e => e.TokenPrefix)
                .HasMaxLength(10);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            // Index on TokenHash for fast lookups during authentication
            entity.HasIndex(e => e.TokenHash)
                .IsUnique();

            // Index on Name for uniqueness check
            entity.HasIndex(e => e.Name);
        });
    }
}
