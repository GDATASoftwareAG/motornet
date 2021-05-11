using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Gzip;
using Xunit;

namespace Motor.Extensions.Compression.Gzip_UnitTest
{
    public class GzipMessageCompressorTests
    {
        [Fact]
        public async Task Compress_SomeUncompressedMessage_CompressedMessage()
        {
            var compressor = CreateCompressor();
            var uncompressed = new byte[] { 1, 2, 3 };

            var compressed = await compressor.CompressAsync(uncompressed, CancellationToken.None);

            Assert.Equal(uncompressed, await DecompressInputAsync(compressed));
        }

        private static GzipMessageCompressor CreateCompressor()
        {
            return new();
        }

        private static async Task<byte[]> DecompressInputAsync(byte[] compressed)
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
