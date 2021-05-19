using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Gzip;
using Xunit;

namespace Motor.Extensions.Compression.Gzip_UnitTest
{
    public class GzipMessageDecompressorTests
    {
        [Fact]
        public async Task Decompress_SomeCompressedMessage_DecompressedMessage()
        {
            var decompressor = CreateDecompressor();
            var decompressedInput = new byte[] { 1, 2, 3 };
            var compressed = await CompressAsync(decompressedInput);

            var decompressed = await decompressor.DecompressAsync(compressed, CancellationToken.None);

            Assert.Equal(decompressedInput, decompressed);
        }

        private static GzipMessageDecompressor CreateDecompressor()
        {
            return new();
        }

        private static async Task<byte[]> CompressAsync(byte[] rawMessage)
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
}
