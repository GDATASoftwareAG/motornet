namespace Motor.Extensions.Hosting
{
    public record DefaultApplicationNameOptions
    {
        public string FullName { get; init; } = "";
        public string Source { get; init; } = "";
    }
}
