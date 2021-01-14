using System;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IApplicationNameService
    {
        string GetVersion();
        string GetLibVersion();
        string GetFullName();
        Uri GetSource();
    }
}
