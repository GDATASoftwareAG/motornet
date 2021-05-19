using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Consumer
{
    public class ConsumerBuilder<T> : IConsumerBuilder<T> where T : notnull
    {
        private readonly IServiceCollection _serviceCollection;

        public ConsumerBuilder(IServiceCollection serviceCollection, HostBuilderContext context)
        {
            _serviceCollection = serviceCollection;
            Context = context;
        }

        public HostBuilderContext Context { get; }

        public void AddConsumer<TConsumer>() where TConsumer : IMessageConsumer<T>
        {
            _serviceCollection.AddTransient(typeof(IMessageConsumer<T>), typeof(TConsumer));
            _serviceCollection.AddTransient<IMessageDecompressor, NoOpMessageDecompressor>();
        }

        public void AddDecompressor<TDecompressor>() where TDecompressor : IMessageDecompressor
        {
            _serviceCollection.AddTransient(typeof(IMessageDecompressor), typeof(TDecompressor));
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
