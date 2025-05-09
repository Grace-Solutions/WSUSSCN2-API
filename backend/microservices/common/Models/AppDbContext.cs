using Microsoft.EntityFrameworkCore;

namespace common.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ApiToken> ApiTokens { get; set; } = null!;
    public DbSet<Update> Updates { get; set; } = null!;
    public DbSet<CabFile> CabFiles { get; set; } = null!;
    public DbSet<SourceCab> SourceCabs { get; set; } = null!;
    public DbSet<SyncHistory> SyncHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApiToken
        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.ToTable("api_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Token).HasColumnName("token").IsRequired();
            entity.Property(e => e.Label).HasColumnName("label").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Permissions).HasColumnName("permissions");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedAt).HasColumnName("last_modified_at");
            entity.Property(e => e.Revoked).HasColumnName("revoked");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        });

        // Configure Update
        modelBuilder.Entity<Update>(entity =>
        {
            entity.ToTable("updates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UpdateId).HasColumnName("update_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Classification).HasColumnName("classification");
            entity.Property(e => e.Product).HasColumnName("product");
            entity.Property(e => e.ProductFamily).HasColumnName("product_family");
            entity.Property(e => e.KbArticleId).HasColumnName("kb_article_id");
            entity.Property(e => e.SecurityBulletinId).HasColumnName("security_bulletin_id");
            entity.Property(e => e.MsrcSeverity).HasColumnName("msrc_severity");
            entity.Property(e => e.Categories).HasColumnName("categories");
            entity.Property(e => e.IsSuperseded).HasColumnName("is_superseded");
            entity.Property(e => e.SupersededBy).HasColumnName("superseded_by");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.LastModified).HasColumnName("last_modified");
            entity.Property(e => e.OsVersion).HasColumnName("os_version");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Configure CabFile
        modelBuilder.Entity<CabFile>(entity =>
        {
            entity.ToTable("cab_files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(e => e.GroupStrategy).HasColumnName("group_strategy").IsRequired();
            entity.Property(e => e.GroupValue).HasColumnName("group_value").IsRequired();
            entity.Property(e => e.MinioPath).HasColumnName("minio_path").IsRequired();
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.UpdateCount).HasColumnName("update_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        });

        // Configure SourceCab
        modelBuilder.Entity<SourceCab>(entity =>
        {
            entity.ToTable("source_cabs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(e => e.MinioPath).HasColumnName("minio_path").IsRequired();
            entity.Property(e => e.Etag).HasColumnName("etag");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.DownloadedAt).HasColumnName("downloaded_at");
            entity.Property(e => e.Processed).HasColumnName("processed");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        });

        // Configure SyncHistory
        modelBuilder.Entity<SyncHistory>(entity =>
        {
            entity.ToTable("sync_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SourceCabId).HasColumnName("source_cab_id");
            entity.Property(e => e.UpdatesAdded).HasColumnName("updates_added");
            entity.Property(e => e.UpdatesModified).HasColumnName("updates_modified");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasOne<SourceCab>()
                .WithMany()
                .HasForeignKey(e => e.SourceCabId);
        });
    }
}
