using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.ContentEncoding.Gzip;
using Xunit;

namespace Motor.Extensions.ContentEncoding.Gzip_UnitTest;

public class GzipMessageDecoderTests
{
    [Fact]
    public async Task Decode_SomeEncodedMessage_DecodedMessage()
    {
        var decoder = CreateDecoder();
        var decodedInput = new byte[] { 1, 2, 3 };
        var encoded = await EncodeAsync(decodedInput);

        var decoded = await decoder.DecodeAsync(encoded, CancellationToken.None);

        Assert.Equal(decodedInput, decoded);
    }

    [Fact]
    public async Task Decode_DecodedMessageTooLarge_ThrowsArgumentException()
    {
        var decoder = CreateDecoder(new ContentEncodingOptions { MaxMessageBytes = 100 });
        var decodedInput = new byte[1000];
        var encoded = await EncodeAsync(decodedInput);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await decoder.DecodeAsync(encoded, CancellationToken.None)
        );
    }

    private static GzipMessageDecoder CreateDecoder(ContentEncodingOptions? options = null)
    {
        return new(Options.Create(options ?? new ContentEncodingOptions()));
    }

    private static async Task<byte[]> EncodeAsync(byte[] rawMessage)
    {
        await using var compressedStream = new MemoryStream();
        //use using with explicit scope to close/flush the GZipStream before using the outputstream
        await using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            await zipStream.WriteAsync(rawMessage, CancellationToken.None);
        }
        return compressedStream.ToArray();
    }
}
