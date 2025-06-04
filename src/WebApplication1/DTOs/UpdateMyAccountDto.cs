using System.ComponentModel.DataAnnotations;
using WebApplication1.Validation;

namespace WebApplication1.DTOs;

public class UpdateMyAccountDto
{
    [Required]
    [UsernameNotStartWithNumber]
    public string Username { get; set; }
    [Required]
    [PasswordComplexity]
    public string Password { get; set; }
    public string FirstName{ get; set; }
    public string MiddleName{ get; set; }
    public string LastName{ get; set; }
    public string Email{ get; set; }
    public string PhoneNumber { get; set; }
    public string PassportNumber{ get; set; }
}
