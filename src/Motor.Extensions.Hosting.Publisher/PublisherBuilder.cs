using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Publisher;

public class PublisherBuilder<TOutput> : IPublisherBuilder<TOutput>
    where TOutput : class
{
    private readonly IServiceCollection _serviceCollection;

    public PublisherBuilder(IServiceCollection serviceCollection, HostBuilderContext context)
    {
        _serviceCollection = serviceCollection;
        Context = context;
    }

    public Type? PublisherImplType { get; private set; }
    public HostBuilderContext Context { get; }

    public void AddPublisher<TPublisher>() where TPublisher : ITypedMessagePublisher<byte[]>
    {
        _serviceCollection.AddTransient(typeof(TPublisher));
        _serviceCollection.AddTransient<IMessageEncoder, NoOpMessageEncoder>();
        PublisherImplType = typeof(TypedMessagePublisher<TOutput, TPublisher>);
    }

    public void AddPublisher<TPublisher>(Func<IServiceProvider, TPublisher> implementationFactory) where TPublisher : class, ITypedMessagePublisher<byte[]>
    {
        _serviceCollection.AddTransient(implementationFactory);
        _serviceCollection.AddTransient<IMessageEncoder, NoOpMessageEncoder>();
        PublisherImplType = typeof(TypedMessagePublisher<TOutput, TPublisher>);
    }

    public void AddSerializer<TSerializer>() where TSerializer : IMessageSerializer<TOutput>
    {
        _serviceCollection.AddTransient(typeof(IMessageSerializer<TOutput>), typeof(TSerializer));
    }

    public void AddEncoder<TEncoder>() where TEncoder : IMessageEncoder
    {
        _serviceCollection.Replace(ServiceDescriptor.Transient(typeof(IMessageEncoder), typeof(TEncoder)));
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
