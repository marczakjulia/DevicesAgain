using System.ComponentModel.DataAnnotations;
using WebApplication1.Validation;

namespace WebApplication1.DTOs;

public class CreateAccountDto
{
    [Required]
    [UsernameNotStartWithNumber]
    public string Username { get; set; }

    [Required]
    [PasswordComplexity]
    public string Password { get; set; }

    [Required]
    public int EmployeeId { get; set; }
}