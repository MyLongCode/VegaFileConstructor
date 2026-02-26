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
    }
}
