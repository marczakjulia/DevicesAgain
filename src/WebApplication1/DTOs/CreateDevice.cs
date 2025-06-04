using System.ComponentModel.DataAnnotations;
using System.Text.Json;


namespace WebApplication1.DTO;

public class CreateDevice
{
    [Required]
    public required string Name { get; set; } = null!;
    [Required]
    public required string DeviceTypeName { get; set; }  = null!;
    
    [Required]
    public required bool IsEnabled {get; set;}
    
    [Required] 
    public required JsonElement AdditionalProperties { get; set; } 
}