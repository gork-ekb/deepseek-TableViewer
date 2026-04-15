using Microsoft.EntityFrameworkCore;
using TableViewer.Models;

namespace TableViewer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ViewConfig> Views { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ViewConfig>(entity =>
        {
            entity.ToTable("views");
            entity.Property(e => e.IsProtected).HasDefaultValue(false);
            entity.Property(e => e.AllowFiltering).HasDefaultValue(true);
            entity.Property(e => e.AllowSorting).HasDefaultValue(true);
        });
    }
}