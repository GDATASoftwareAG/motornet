using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Abstractions;
using Xunit;

namespace Motor.Extensions.Compression.Abstractions_UnitTest
{
    public class NoOpDecompressorTests
    {
        [Fact]
        public async Task Decompress_SomeCompressedMessage_DecompressionDoesNotChangeInput()
        {
            var decompressor = CreateDecompressor();
            var compressed = new byte[] { 1, 2, 3 };

            var uncompressed = await decompressor.DecompressAsync(compressed, CancellationToken.None);

            Assert.Equal(compressed, uncompressed);
        }

        private static NoOpMessageDecompressor CreateDecompressor()
        {
            return new();
        }
    }
}
