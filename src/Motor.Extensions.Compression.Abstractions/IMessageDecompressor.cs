using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Compression.Abstractions
{
    public interface IMessageDecompressor
    {
        string CompressionType { get; }
        Task<byte[]> DecompressAsync(byte[] compressedMessage, CancellationToken cancellationToken);
    }
}
