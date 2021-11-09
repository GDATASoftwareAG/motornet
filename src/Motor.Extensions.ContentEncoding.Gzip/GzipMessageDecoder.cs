using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.ContentEncoding.Abstractions;

namespace Motor.Extensions.ContentEncoding.Gzip;

public class GzipMessageDecoder : IMessageDecoder
{
    public string Encoding => GzipMessageEncoder.GzipEncoding;

    public async Task<byte[]> DecodeAsync(byte[] encodedMessage, CancellationToken cancellationToken)
    {
        var compressedStream = new MemoryStream(encodedMessage);

        var output = new MemoryStream();
        //use using with explicit scope to close/flush the GZipStream before using the outputstream
        await using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            await decompressionStream.CopyToAsync(output, cancellationToken);
        }

        return output.ToArray();
    }
}
