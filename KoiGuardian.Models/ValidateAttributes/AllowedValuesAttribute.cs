using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.ValidateAttributes
{
    public class AllowedValuesAttribute : ValidationAttribute
    {
        private readonly int[] _allowedValues;

        public AllowedValuesAttribute(params int[] allowedValues)
        {
            _allowedValues = allowedValues;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is int intValue && !_allowedValues.Contains(intValue))
            {
                return new ValidationResult($"Value must be one of the following: {string.Join(", ", _allowedValues)}.");
            }

            return ValidationResult.Success;
        }
    }


}
