using System;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IApplicationNameService
    {
        string GetProduct();
        string GetVersion();
        string GetLibVersion();
        string GetFullName();
        Uri GetSource();
    }
}
