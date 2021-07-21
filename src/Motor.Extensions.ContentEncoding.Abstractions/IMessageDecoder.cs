using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.ContentEncoding.Abstractions
{
    public interface IMessageDecoder
    {
        string Encoding { get; }
        Task<byte[]> DecodeAsync(byte[] encodedMessage, CancellationToken cancellationToken);
    }
}
