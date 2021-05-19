using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Abstractions;
using Xunit;

namespace Motor.Extensions.Compression.Abstractions_UnitTest
{
    public class NoOpCompressorTests
    {
        [Fact]
        public async Task Compress_SomeMessage_CompressionDoesNotChangeInput()
        {
            var compressor = CreateCompressor();
            var uncompressed = new byte[] { 1, 2, 3 };

            var compressed = await compressor.CompressAsync(uncompressed, CancellationToken.None);

            Assert.Equal(uncompressed, compressed);
        }

        private static NoOpMessageCompressor CreateCompressor()
        {
            return new();
        }
    }
}
