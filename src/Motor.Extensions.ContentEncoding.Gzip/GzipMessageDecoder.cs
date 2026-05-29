using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Motor.Extensions.ContentEncoding.Abstractions;

namespace Motor.Extensions.ContentEncoding.Gzip;

public class GzipMessageDecoder : IMessageDecoder
{
    private readonly ContentEncodingOptions _options;

    public GzipMessageDecoder(IOptions<ContentEncodingOptions> options)
    {
        _options = options.Value;
    }

    public string Encoding => GzipMessageEncoder.GzipEncoding;

    public async Task<byte[]> DecodeAsync(byte[] encodedMessage, CancellationToken cancellationToken)
    {
        var compressedStream = new MemoryStream(encodedMessage);

        var output = new MemoryStream();
        //use using with explicit scope to close/flush the GZipStream before using the output stream
        await using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            var buffer = new byte[64 * 1024];
            var totalBytesRead = 0L;
            int bytesRead;
            while ((bytesRead = await decompressionStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                totalBytesRead += bytesRead;
                if (totalBytesRead > _options.MaxMessageBytes)
                {
                    throw new ArgumentException(
                        $"Decompressed Message size {encodedMessage.Length} exceeds the maximum allowed size of {_options.MaxMessageBytes} bytes."
                    );
                }
                await output.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            }
            await decompressionStream.CopyToAsync(output, cancellationToken);
        }

        return output.ToArray();
    }
}
