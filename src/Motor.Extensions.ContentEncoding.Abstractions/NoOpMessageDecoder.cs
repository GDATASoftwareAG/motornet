using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.ContentEncoding.Abstractions
{
    public class NoOpMessageDecoder : IMessageDecoder
    {
        public string Encoding => NoOpMessageEncoder.NoEncoding;

        public Task<byte[]> DecodeAsync(byte[] encodedMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(encodedMessage);
        }
    }
}
