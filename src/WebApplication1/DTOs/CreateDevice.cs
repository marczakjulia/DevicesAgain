using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WebApplication1.DTO;

public class CreateDevice
{
    [Required]
    public required string Name { get; set; }
    [Required]
    public required bool IsEnabled { get; set; }
    [Required]
    public required JsonElement AdditionalProperties { get; set; }
    [Required]
    public required int TypeId { get; set; }
}