using System;
using System.Text;
using Motor.Extensions.Conversion.SystemJson;
using Newtonsoft.Json;
using Xunit;

namespace Motor.Extensions.Conversion.SystemJson_UnitTest;

public class JsonSerializerTests
{
    private InputMessage ValidMessage => new() { Firstname = "Foo", Lastname = "Bar", Age = 42 };

    [Fact]
    public void Serialize_ValidMessage_SerializedMessage()
    {
        var serializer = CreateSerializer();
        var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ValidMessage));

        var message = serializer.Serialize(ValidMessage);

        Assert.Equal(expected, message);
    }

    [Fact]
    public void Serialize_NullMessage_Throw()
    {
        var serializer = CreateSerializer();

        Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
    }

    private SystemJsonSerializer<InputMessage> CreateSerializer()
    {
        return new();
    }
}
