using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VegaFileConstructor.Models;

namespace VegaFileConstructor.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<TemplateFieldDefinition> TemplateFieldDefinitions => Set<TemplateFieldDefinition>();
    public DbSet<TemplateFieldPlacement> TemplateFieldPlacements => Set<TemplateFieldPlacement>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<GenerationFieldValue> GenerationFieldValues => Set<GenerationFieldValue>();
    public DbSet<PdfEditOperation> PdfEditOperations => Set<PdfEditOperation>();
    public DbSet<PdfEditReplacement> PdfEditReplacements => Set<PdfEditReplacement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DocumentTemplate>()
            .HasIndex(t => t.Code)
            .IsUnique();

        builder.Entity<TemplateFieldDefinition>()
            .HasIndex(x => new { x.TemplateId, x.Key })
            .IsUnique();

        builder.Entity<TemplateFieldPlacement>()
            .HasIndex(x => new { x.TemplateId, x.FieldKey });

        builder.Entity<PdfEditOperation>()
            .HasIndex(x => new { x.UserId, x.CreatedAt });

        builder.Entity<PdfEditReplacement>()
            .HasIndex(x => new { x.OperationId, x.Order });

        builder.Entity<PdfEditReplacement>()
            .HasOne(x => x.Operation)
            .WithMany(x => x.Replacements)
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
