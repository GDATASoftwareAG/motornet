using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Compression.Abstractions
{

    public class NoOpMessageCompressor : IMessageCompressor
    {
        public string CompressionType => NoOpCompressionType;

        public const string NoOpCompressionType = "uncompressed";

        public Task<byte[]> CompressAsync(byte[] rawMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(rawMessage);
        }
    }
}
