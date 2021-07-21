using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.ContentEncoding.Gzip;
using Xunit;

namespace Motor.Extensions.ContentEncoding.Gzip_UnitTest
{
    public class GzipMessageEncoderTests
    {
        [Fact]
        public async Task Encode_SomeUnencodedMessage_EncodedMessage()
        {
            var encoder = CreateEncoder();
            var unencoded = new byte[] { 1, 2, 3 };

            var encoded = await encoder.EncodeAsync(unencoded, CancellationToken.None);

            Assert.Equal(unencoded, await DecodeInputAsync(encoded));
        }

        private static GzipMessageEncoder CreateEncoder()
        {
            return new();
        }

        private static async Task<byte[]> DecodeInputAsync(byte[] compressed)
        {
            var compressedStream = new MemoryStream(compressed);

            var output = new MemoryStream();
            //use using with explicit scope to close/flush the GZipStream before using the outputstream
            await using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(output, CancellationToken.None);
            }

            return output.ToArray();
        }
    }
}
