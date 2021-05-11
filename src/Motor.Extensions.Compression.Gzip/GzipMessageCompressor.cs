using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Abstractions;

namespace Motor.Extensions.Compression.Gzip
{
    public class GzipMessageCompressor : IMessageCompressor
    {
        public string CompressionType => GzipCompressionType;

        public const string GzipCompressionType = "gzip";

        public async Task<byte[]> CompressAsync(byte[] rawMessage, CancellationToken cancellationToken)
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
