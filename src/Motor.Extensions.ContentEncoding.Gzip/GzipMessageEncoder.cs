using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.ContentEncoding.Abstractions;

namespace Motor.Extensions.ContentEncoding.Gzip
{
    public class GzipMessageEncoder : IMessageEncoder
    {
        public string Encoding => GzipEncoding;

        public static string GzipEncoding => "gzip";

        public async Task<byte[]> EncodeAsync(byte[] rawMessage, CancellationToken cancellationToken)
        {
            await using var compressedStream = new MemoryStream();
            //use using with explicit scope to close/flush the GZipStream before using the outputstream
            await using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await zipStream.WriteAsync(rawMessage, cancellationToken);
            }
            return compressedStream.ToArray();
        }
    }
}
