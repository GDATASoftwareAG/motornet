using System.ComponentModel.DataAnnotations;
using Motor.Extensions.Hosting.RabbitMQ.Validation;
using Xunit;
using static System.ComponentModel.DataAnnotations.Validator;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest.Validation;

public class NotWhitespaceOrEmptyAttributeTests
{
    record Demo
    {
        [NotWhitespaceOrEmpty]
        public string Name { get; init; }
    }

    [Fact]
    public void Validate_ValidObject_NoException()
    {
        var toValidate = new Demo { Name = "Ted" };

        ValidateObject(toValidate, new ValidationContext(toValidate));
    }

    [Fact]
    public void Validate_StringEmpty_ValidationException()
    {
        var toValidate = new Demo { Name = string.Empty };

        Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
    }

    [Fact]
    public void Validate_SolelyWhitespace_ValidationException()
    {
        var toValidate = new Demo { Name = "   " };

        Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
    }

    [Fact]
    public void Validate_Null_ValidationException()
    {
        var toValidate = new Demo();

        Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
    }
}
