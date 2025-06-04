namespace WebApplication1.DTOs;
public class EmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public PositionDto Position { get; set; }
    public PersonDto Person { get; set; }
}