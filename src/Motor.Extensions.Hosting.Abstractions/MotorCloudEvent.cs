using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using CloudNative.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions
{
    public class MotorCloudEvent<TData> : CloudEvent
        where TData : class
    {
        private readonly IApplicationNameService _applicationNameService;

        private MotorCloudEvent(
            IApplicationNameService applicationNameService,
            TData data,
            Uri source,
            string? id = null,
            DateTime? time = null,
            params ICloudEventExtension[] extensions)
            : this(applicationNameService, data, typeof(TData).Name, source, id, time, extensions)
        {
        }

        public MotorCloudEvent(
            IApplicationNameService applicationNameService,
            TData data,
            string type,
            Uri source,
            string? id = null,
            DateTime? time = null,
            params ICloudEventExtension[] extensions)
            : base(CloudEventsSpecVersion.Default, type, source, id, time, extensions)
        {
            _applicationNameService = applicationNameService;
            TypedData = data;
            DataContentType = new ContentType();
        }

        public MotorCloudEvent(
            IApplicationNameService applicationNameService,
            TData data,
            IEnumerable<ICloudEventExtension> extensions)
            : base(CloudEventsSpecVersion.Default, extensions)
        {
            _applicationNameService = applicationNameService;
            TypedData = data;
        }

        public TData TypedData
        {
            get => (TData)Data;
            set => Data = value;
        }

        public Dictionary<Type, ICloudEventExtension> GetExtensions()
        {
            return Extensions;
        }

        public T GetExtensionOrCreate<T>(Func<T> createNewExtension)
            where T : ICloudEventExtension
        {
            if (Extensions.TryGetValue(typeof(T), out var cloudEventExtension)) return (T)cloudEventExtension;

            var invoke = createNewExtension.Invoke();
            Extensions.Add(invoke.GetType(), invoke);
            invoke.Attach(this);
            return invoke;
        }

        private static MotorCloudEvent<T> CreateCloudEvent<T>(IApplicationNameService applicationNameService, T data,
            IEnumerable<ICloudEventExtension>? extensions = null)
            where T : class
        {
            return new(applicationNameService, data, applicationNameService.GetSource(),
                extensions: extensions?.ToArray() ?? new ICloudEventExtension[0]);
        }

        public MotorCloudEvent<T> CreateNew<T>(T data, bool useOldIdentifier = false)
            where T : class
        {
            var cloudEvent = useOldIdentifier
                ? new MotorCloudEvent<T>(_applicationNameService, data, Type, Source, Id, Time,
                    Extensions.Select(t => t.Value).ToArray())
                : CreateCloudEvent(_applicationNameService, data, Extensions.Select(t => t.Value));
            var newAttributes = cloudEvent.GetAttributes();
            foreach (var attribute in GetAttributes())
            {
                if (!newAttributes.ContainsKey(attribute.Key))
                {
                    newAttributes.Add(attribute.Key, attribute.Value);
                }
            }
            return cloudEvent;
        }
    }
}
