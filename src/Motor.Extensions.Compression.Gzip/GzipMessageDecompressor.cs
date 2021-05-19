using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Abstractions;

namespace Motor.Extensions.Compression.Gzip
{
    public class GzipMessageDecompressor : IMessageDecompressor
    {
        public string CompressionType => GzipMessageCompressor.GzipCompressionType;

        public async Task<byte[]> DecompressAsync(byte[] compressedMessage, CancellationToken cancellationToken)
        {
            var compressedStream = new MemoryStream(compressedMessage);

            var output = new MemoryStream();
            //use using with explicit scope to close/flush the GZipStream before using the outputstream
            await using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(output, cancellationToken);
            }

            return output.ToArray();
        }
    }
}
