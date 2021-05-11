using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Compression.Abstractions
{
    public interface IMessageCompressor
    {
        string CompressionType { get; }
        Task<byte[]> CompressAsync(byte[] rawMessage, CancellationToken cancellationToken);
    }
}
