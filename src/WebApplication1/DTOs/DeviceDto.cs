namespace WebApplication1.DTOs;

public class DeviceDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DeviceDto(int id, string name)
    {
        Id = id;
        Name = name;
    }
    
}