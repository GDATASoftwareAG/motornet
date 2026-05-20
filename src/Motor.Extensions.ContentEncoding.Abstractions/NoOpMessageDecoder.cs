using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Motor.Extensions.ContentEncoding.Abstractions;

public class NoOpMessageDecoder : IMessageDecoder
{
    private readonly ContentEncodingOptions _options;

    public NoOpMessageDecoder(IOptions<ContentEncodingOptions> options)
    {
        _options = options.Value;
    }

    public string Encoding => NoOpMessageEncoder.NoEncoding;

    public Task<byte[]> DecodeAsync(byte[] encodedMessage, CancellationToken cancellationToken)
    {
        return encodedMessage.Length > _options.MaxMessageBytes
            ? throw new ArgumentException(
                $"Message size {encodedMessage.Length} exceeds the maximum allowed size of {_options.MaxMessageBytes} bytes."
            )
            : Task.FromResult(encodedMessage);
    }
}
