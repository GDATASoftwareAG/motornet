namespace Motor.Extensions.ContentEncoding.Abstractions;

public record ContentEncodingOptions
{
    public bool IgnoreEncoding { get; init; }
    public long MaxMessageBytes { get; init; } = 100 * 1024 * 1024;
}
