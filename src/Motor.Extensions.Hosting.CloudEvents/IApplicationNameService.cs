using System;

namespace Motor.Extensions.Hosting.CloudEvents
{
    public interface IApplicationNameService
    {
        string GetVersion();
        string GetLibVersion();
        string GetFullName();
        Uri GetSource();
    }
}
