using System;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IApplicationNameService
    {
        [Obsolete("Will be removed in the next minor version")]
        string GetProduct();
        string GetVersion();
        string GetLibVersion();
        string GetFullName();
        Uri GetSource();
    }
}
