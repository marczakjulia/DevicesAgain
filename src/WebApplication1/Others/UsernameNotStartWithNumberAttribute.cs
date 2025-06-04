using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Validation
{
    public class UsernameNotStartWithNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string username)
            {
                if (!string.IsNullOrEmpty(username) && char.IsDigit(username[0]))
                {
                    return new ValidationResult("Username must not start with a number");
                }
            }
            return ValidationResult.Success;
        }
    }
}