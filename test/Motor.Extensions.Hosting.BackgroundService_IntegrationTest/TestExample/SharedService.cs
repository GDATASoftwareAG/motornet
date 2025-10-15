namespace Motor.Extensions.Hosting.BackgroundService_IntegrationTest.TestExample;

public interface ISharedService
{
    public void Start();
    public bool IsStarted();
    public void Finish();
    public bool IsFinished();
}

public class SharedService : ISharedService
{
    private bool _started;
    private bool _finished;

    public void Start()
    {
        _started = true;
    }

    public bool IsStarted()
    {
        return _started;
    }

    public void Finish()
    {
        _finished = true;
    }

    public bool IsFinished()
    {
        return _finished;
    }
}
