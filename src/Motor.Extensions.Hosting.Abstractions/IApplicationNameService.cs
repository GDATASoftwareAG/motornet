using System;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IApplicationNameService
    {
        string GetProduct();
        string GetVersion();
        string GetLibVersion();
        [Obsolete("Will be removed in the next minor version")]
        string GetFullName();
        Uri GetSource();
    }
}
