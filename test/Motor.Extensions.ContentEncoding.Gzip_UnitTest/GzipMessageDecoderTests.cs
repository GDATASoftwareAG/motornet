using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
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

    private static GzipMessageDecoder CreateDecoder()
    {
        return new();
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
