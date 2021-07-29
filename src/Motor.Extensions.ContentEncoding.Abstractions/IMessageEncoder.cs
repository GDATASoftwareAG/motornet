using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.ContentEncoding.Abstractions
{
    public interface IMessageEncoder
    {
        string Encoding { get; }
        Task<byte[]> EncodeAsync(byte[] rawMessage, CancellationToken cancellationToken);
    }
}
