using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Motor.Extensions.Hosting.RabbitMQ.Validation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RequireValidAttribute : RequiredAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value switch
        {
            null => new ValidationResult($"Value of {validationContext.DisplayName} must not be null"),
            IEnumerable e => ValidateCollection(e, validationContext),
            { } => ValidateObject(value, validationContext),
        };

        private ValidationResult? ValidateCollection(IEnumerable e, ValidationContext validationContext)
        {
            foreach (var value in e)
            {
                var result = ValidateObject(value, validationContext);
                if (result != ValidationResult.Success) return result;
            }

            return ValidationResult.Success;
        }

        private ValidationResult? ValidateObject(object value, ValidationContext validationContext)
        {
            try
            {
                Validator.ValidateObject(value,
                    new ValidationContext(value, validationContext, validationContext.Items), true);
            }
            catch (ValidationException e)
            {
                return e.ValidationResult;
            }

            return ValidationResult.Success;
        }
    }
}
