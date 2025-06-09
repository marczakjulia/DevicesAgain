namespace WebApplication1.DTOs;

public class DeviceTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public DeviceTypeDto(int id, string name)
    {
        Id = id;
        Name = name;
    }
}