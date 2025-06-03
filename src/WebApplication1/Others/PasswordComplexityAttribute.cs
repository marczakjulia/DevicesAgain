using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Validation
{
    public class PasswordComplexityAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string password)
            {
                if (password.Length < 12)
                {
                    return new ValidationResult("Password must be at least 12 characters");
                }
                if (!Regex.IsMatch(password, @"[a-z]"))
                {
                    return new ValidationResult("Password must have at least one lowercase letter.");
                }
                if (!Regex.IsMatch(password, @"[A-Z]"))
                {
                    return new ValidationResult("Password must have at least one uppercase letter");
                }
                if (!Regex.IsMatch(password, @"\d"))
                {
                    return new ValidationResult("Password have contain at least one digit");
                }
                if (!Regex.IsMatch(password, @"[^a-zA-Z\d]"))
                {
                    return new ValidationResult("Password must have at least one symbol");
                }
            }
            return ValidationResult.Success;
        }
    }
}