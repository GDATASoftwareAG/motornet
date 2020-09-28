using System;
using System.Collections;
using System.Collections.Generic;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Hosting.Consumer
{
    public class ConsumerBuilder<T> : IConsumerBuilder<T>
    {
        private readonly IServiceCollection _serviceCollection;

        public HostBuilderContext Context { get; }

        public ConsumerBuilder(IServiceCollection serviceCollection, HostBuilderContext context)
        {
            _serviceCollection = serviceCollection;
            Context = context;
        }

        public void AddConsumer<TConsumer>() where TConsumer : IMessageConsumer<T>
        {
            _serviceCollection.AddTransient(typeof(IMessageConsumer<T>), typeof(TConsumer));
        }

        public void AddDeserializer<TDeserializer>() where TDeserializer : IMessageDeserializer<T>
        {
            _serviceCollection.AddTransient(typeof(IMessageDeserializer<T>), typeof(TDeserializer));
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _serviceCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ServiceDescriptor item)
        {
            _serviceCollection.Add(item);
        }

        public void Clear()
        {
            _serviceCollection.Clear();
        }

        public bool Contains(ServiceDescriptor item)
        {
            return _serviceCollection.Contains(item);
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _serviceCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(ServiceDescriptor item)
        {
            return _serviceCollection.Remove(item);
        }

        public int Count => _serviceCollection.Count;
        public bool IsReadOnly => _serviceCollection.IsReadOnly;
        public int IndexOf(ServiceDescriptor item)
        {
            return _serviceCollection.IndexOf(item);
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            _serviceCollection.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _serviceCollection.RemoveAt(index);
        }

        public ServiceDescriptor this[int index]
        {
            get => _serviceCollection[index];
            set => _serviceCollection[index] = value;
        }
    }
}
