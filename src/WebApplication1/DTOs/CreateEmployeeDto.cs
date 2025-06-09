namespace WebApplication1.DTOs;

public class CreateEmployeeDto
{
    public PersonDto Person { get; set; }
    public decimal Salary { get; set; }
    public int PositionId { get; set; }
}

