using System.ComponentModel.DataAnnotations;
using Motor.Extensions.Hosting.RabbitMQ.Validation;
using Xunit;
using static System.ComponentModel.DataAnnotations.Validator;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest.Validation
{
    public class RequireValidAttributeTests
    {
        record Nested
        {
            [Required(AllowEmptyStrings = false)]
            public string Greeting { get; init; }
        }

        record Wrapper
        {
            [RequireValid]
            public Nested Nested { get; init; }
        }

        [Fact]
        public void Validate_ValidObject_NoException()
        {
            var toValidate = new Wrapper
            {
                Nested = new Nested
                {
                    Greeting = "Hello"
                }
            };

            ValidateObject(toValidate, new ValidationContext(toValidate));
        }

        [Fact]
        public void Validate_InValidObject_ValidationException()
        {
            var toValidate = new Wrapper
            {
                Nested = new Nested()
            };

            Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
        }

        [Fact]
        public void Validate_Null_ValidationException()
        {
            var toValidate = new Wrapper();
            Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
        }
    }
}
