using System.ComponentModel.DataAnnotations;

namespace Contoso.Transactions.Services
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NonZeroDecimalAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue == 0)
                {
                    return new ValidationResult("O valor não pode ser 0.");
                }
                return ValidationResult.Success;
            }

            return new ValidationResult("Valor inválido.");
        }
    }
}
