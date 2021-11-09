using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.ContentEncoding.Abstractions;
using Xunit;

namespace Motor.Extensions.ContentEncoding.Abstractions_UnitTest;

public class NoOpEncoderTests
{
    [Fact]
    public async Task Encode_SomeMessage_EncodingDoesNotChangeInput()
    {
        var encoder = CreateEncoder();
        var unencoded = new byte[] { 1, 2, 3 };

        var encoded = await encoder.EncodeAsync(unencoded, CancellationToken.None);

        Assert.Equal(unencoded, encoded);
    }

    private static NoOpMessageEncoder CreateEncoder()
    {
        return new();
    }
}
