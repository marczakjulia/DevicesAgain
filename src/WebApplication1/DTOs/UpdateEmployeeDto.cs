namespace WebApplication1.DTOs;

public class UpdateEmployeeDto
{
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string PassportNumber { get; set; }
    public int? PositionId { get; set; }
    public decimal? Salary { get; set; } 
} 