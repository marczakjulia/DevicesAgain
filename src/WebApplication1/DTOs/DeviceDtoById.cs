namespace WebApplication1.DTOs;

using System.Text.Json;
public class DeviceDtoById
{
    public string Name { get; set; } = null!;
    public bool IsEnabled { get; set; }

    public JsonElement AdditionalProperties { get; set; }

    public string DeviceTypeName { get; set; }
    
    public CurrentUserDTO? Employee { get; set; }
}