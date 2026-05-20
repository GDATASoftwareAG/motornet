using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task Decode_MessageTooLarge_ThrowsArgumentException()
    {
        var decoder = CreateDecoder(new ContentEncodingOptions { MaxMessageBytes = 2 });
        var encoded = new byte[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await decoder.DecodeAsync(encoded, CancellationToken.None)
        );
    }

    private static NoOpMessageDecoder CreateDecoder(ContentEncodingOptions? options = null)
    {
        return new(Options.Create(options ?? new ContentEncodingOptions()));
    }
}
