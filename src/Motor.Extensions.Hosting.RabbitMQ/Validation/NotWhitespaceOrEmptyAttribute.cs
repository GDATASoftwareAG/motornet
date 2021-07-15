using System;
using System.ComponentModel.DataAnnotations;

namespace Motor.Extensions.Hosting.RabbitMQ.Validation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class NotWhitespaceOrEmptyAttribute : RequiredAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value switch
        {
            null => new ValidationResult($"value of {validationContext.DisplayName} must not be empty"),
            string s when string.IsNullOrWhiteSpace(s) => new ValidationResult(
                $"value of {validationContext.DisplayName} must not be solely whitespace"),
            _ => ValidationResult.Success
        };

        public override bool RequiresValidationContext => true;
    }
}
