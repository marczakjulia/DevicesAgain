using System.ComponentModel.DataAnnotations;


namespace WebApplication1.DTOs;

public class CreateAccountDto
{
    [Required]
    [RegularExpression(@"^[^\d].*", ErrorMessage = "Username must not start with a number.")]
    public required string Username { get; set; }

    [Required]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d]).{12,}$",
        ErrorMessage = "Password must be at least 12 characters long and contain at least one lowercase letter, one uppercase letter, one digit, and one symbol."
    )]
    public required string Password { get; set; }

    [Required]
    public int EmployeeId { get; set; }
}