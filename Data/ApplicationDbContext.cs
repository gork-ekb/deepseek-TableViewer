using Microsoft.EntityFrameworkCore;
using TableViewer.Models;

namespace TableViewer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ViewConfig> Views { get; set; }
    public DbSet<Setting> Settings { get; set; }  // ДОБАВИТЬ ЭТУ СТРОКУ

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

        // Настройки для таблицы settings
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("settings");
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}