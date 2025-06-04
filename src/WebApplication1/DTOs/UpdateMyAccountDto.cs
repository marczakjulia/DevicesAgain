using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs;

public class UpdateMyAccountDto
{
    [RegularExpression(@"^[^\d].*", ErrorMessage = "Username must not start with a number.")]
    public string Username { get; set; }
    
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d]).{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one lowercase letter, one uppercase letter, one digit, and one symbol.")]
    public string Password { get; set; }
    public string FirstName{ get; set; }
    public string MiddleName{ get; set; }
    public string LastName{ get; set; }
    public string Email{ get; set; }
    public string PhoneNumber { get; set; }
    public string PassportNumber{ get; set; }
}
