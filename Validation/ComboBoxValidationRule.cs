using System.Globalization;
using System.Windows.Controls;

namespace LessonsManager.Validation
{
    public class ComboBoxValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // If the value is null or empty, return an error
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(false, "Please select a value");
            }

            // If we get here, the value is valid
            return ValidationResult.ValidResult;
        }
    }
}