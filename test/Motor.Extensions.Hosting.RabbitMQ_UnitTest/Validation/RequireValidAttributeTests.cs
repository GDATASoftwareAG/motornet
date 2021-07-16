using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Motor.Extensions.Hosting.RabbitMQ.Validation;
using Xunit;
using static System.ComponentModel.DataAnnotations.Validator;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest.Validation
{
    public class RequireValidAttributeTests
    {
        [Theory]
        [MemberData(nameof(ValidObjects))]
        public void Validate_ValidObject_NoException(Wrapper toValidate)
        {
            ValidateObject(toValidate, new ValidationContext(toValidate));
        }

        [Theory]
        [MemberData(nameof(InvalidObjects))]
        public void Validate_InValidObject_ValidationException(Wrapper toValidate)
        {
            if (toValidate == null)
            {
                Assert.Throws<ArgumentNullException>(
                    () => ValidateObject(toValidate, new ValidationContext(toValidate)));
            }
            else
            {
                Assert.Throws<ValidationException>(() => ValidateObject(toValidate, new ValidationContext(toValidate)));
            }
        }

        public record Nested
        {
            [Required(AllowEmptyStrings = false)]
            public string Greeting { get; init; }
        }

        public record Wrapper
        {
            [RequireValid]
            public Nested Nested { get; init; }

            [RequireValid]
            public List<Nested> NestedList { get; init; } = new();

            [RequireValid]
            public Nested[] NestedArray { get; init; } = Array.Empty<Nested>();

            [RequireValid]
            public ICollection<Nested> NestedCollection { get; init; } = new List<Nested>();
        }

        public static IEnumerable<object[]> InvalidObjects => new[]
        {
            new[] {null as Wrapper},
            new[] {new Wrapper()},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedArray = null}},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedList = null}},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedCollection = null}},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedArray = new[] {new Nested()}}},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedList = new List<Nested> {new()}}},
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}, NestedCollection = new List<Nested> {new()}}},
        };

        public static IEnumerable<object[]> ValidObjects => new[]
        {
            new[] {new Wrapper {Nested = new Nested {Greeting = "Hello"}}},
            new[]
            {
                new Wrapper
                {
                    Nested = new Nested {Greeting = "Hello"},
                    NestedArray = new[] {new Nested {Greeting = "Hello"}}
                }
            },
            new[]
            {
                new Wrapper
                {
                    Nested = new Nested {Greeting = "Hello"},
                    NestedList = new List<Nested> {new() {Greeting = "Hello"}}
                }
            },
            new[]
            {
                new Wrapper
                {
                    Nested = new Nested {Greeting = "Hello"},
                    NestedCollection = new List<Nested> {new() {Greeting = "Hello"}}
                }
            },
        };
    }
}
