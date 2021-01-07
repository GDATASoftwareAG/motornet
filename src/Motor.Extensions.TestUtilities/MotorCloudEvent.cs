using System;
using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.TestUtilities
{
    public class MotorCloudEvent
    {
        public static MotorCloudEvent<T> CreateTestCloudEvent<T>(T data, Uri? source = null,
            IEnumerable<ICloudEventExtension>? extensions = null)
            where T : class
        {
            var applicationNameService = new TestApplicationNameService(source);
            return new MotorCloudEvent<T>(applicationNameService, data, typeof(T).Name,
                applicationNameService.GetSource(), extensions: extensions?.ToArray() ?? Array.Empty<ICloudEventExtension>());
        }

        private class TestApplicationNameService : IApplicationNameService
        {
            private readonly Uri _source;

            public TestApplicationNameService(Uri? source)
            {
                _source = source ?? new Uri("motor://test");
            }

            public string GetProduct()
            {
                throw new NotImplementedException();
            }

            public string GetVersion()
            {
                throw new NotImplementedException();
            }

            public string GetLibVersion()
            {
                throw new NotImplementedException();
            }

            public string GetFullName()
            {
                throw new NotImplementedException();
            }

            public Uri GetSource()
            {
                return _source;
            }
        }
    }
}
