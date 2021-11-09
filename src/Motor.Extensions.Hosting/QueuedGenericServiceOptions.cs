namespace Motor.Extensions.Hosting;

public record QueuedGenericServiceOptions
{
    public int? ParallelProcesses { get; set; } = null;
}
