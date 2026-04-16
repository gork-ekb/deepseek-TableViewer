using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TableViewer.Models;

[Table("settings")]
public class Setting
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    [Column("value")]
    public string? Value { get; set; }
}