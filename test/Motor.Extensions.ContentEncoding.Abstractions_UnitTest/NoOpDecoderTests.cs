using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.ContentEncoding.Abstractions;
using Xunit;

namespace Motor.Extensions.ContentEncoding.Abstractions_UnitTest;

public class NoOpDecoderTests
{
    [Fact]
    public async Task Decode_SomeEncodedMessage_DecodingDoesNotChangeInput()
    {
        var decoder = CreateDecoder();
        var encoded = new byte[] { 1, 2, 3 };

        var decoded = await decoder.DecodeAsync(encoded, CancellationToken.None);

        Assert.Equal(encoded, decoded);
    }

    private static NoOpMessageDecoder CreateDecoder()
    {
        return new();
    }
}
