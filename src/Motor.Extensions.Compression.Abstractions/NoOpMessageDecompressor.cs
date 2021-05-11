using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Compression.Abstractions
{
    public class NoOpMessageDecompressor : IMessageDecompressor
    {
        public string CompressionType => NoOpMessageCompressor.NoOpCompressionType;

        public Task<byte[]> DecompressAsync(byte[] compressedMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(compressedMessage);
        }
    }
}
