using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TableViewer.Models;

[Table("views")]
public class ViewConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("view_group")]
    [MaxLength(50)]
    public string? Group { get; set; }

    [Column("header")]
    [Required]
    [MaxLength(100)]
    public required string Header { get; set; }

    [Column("link")]
    [Required]
    [MaxLength(50)]
    public required string Link { get; set; }

    [Column("host")]
    [Required]
    [MaxLength(50)]
    public required string Host { get; set; }

    [Column("database_name")]
    [Required]
    [MaxLength(50)]
    public required string DatabaseName { get; set; }

    [Column("schema_name")]
    [Required]
    [MaxLength(50)]
    public required string SchemaName { get; set; }

    [Column("table_name")]
    [Required]
    [MaxLength(50)]
    public required string TableName { get; set; }

    [Column("is_protected")]
    public bool IsProtected { get; set; }

    [Column("allow_filtering")]
    public bool AllowFiltering { get; set; }

    [Column("allow_sorting")]
    public bool AllowSorting { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_simple")]
    public bool IsSimple { get; set; }

    [Column("default_sort_field")]
    [MaxLength(50)]
    public string? DefaultSortField { get; set; }

    [Column("page_size")]
    public int PageSize { get; set; } = 0;
}