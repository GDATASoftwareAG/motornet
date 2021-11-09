namespace Motor.Extensions.Hosting.Timer;

public record TimerOptions
{
    public string Days { get; set; } = "*";
    public string Hours { get; set; } = "0";
    public string Minutes { get; set; } = "0";
    public string Seconds { get; set; } = "0";

    public string GetCronString()
    {
        var cronString = $"{Seconds} {Minutes} {Hours} {Days} * ?";
        return cronString;
    }
}
