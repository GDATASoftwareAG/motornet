using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.ContentEncoding.Abstractions
{

    public class NoOpMessageEncoder : IMessageEncoder
    {
        public string Encoding => NoEncoding;

        public static string NoEncoding => "identity";

        public Task<byte[]> EncodeAsync(byte[] rawMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(rawMessage);
        }
    }
}
