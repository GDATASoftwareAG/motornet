namespace Motor.Extensions.Hosting.Timer.Config
{
    public class TimerConfig
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
}
