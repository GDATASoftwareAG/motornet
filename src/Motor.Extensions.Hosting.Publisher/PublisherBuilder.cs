using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
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
    private const string PublisherSection = "Publisher";

    public PublisherBuilder(IServiceCollection serviceCollection, HostBuilderContext context)
    {
        _serviceCollection = serviceCollection;
        HostingEnvironment = context.HostingEnvironment;
        Configuration = context.Configuration;
        serviceCollection.Configure<PublisherOptions>(context.Configuration.GetSection(PublisherSection));
    }

    public PublisherBuilder(
        IServiceCollection serviceCollection,
        IHostEnvironment hostingEnvironment,
        IConfiguration configuration
    )
    {
        _serviceCollection = serviceCollection;
        HostingEnvironment = hostingEnvironment;
        Configuration = configuration;
        serviceCollection.Configure<PublisherOptions>(configuration.GetSection(PublisherSection));
    }

    public IHostEnvironment HostingEnvironment { get; }

    public IConfiguration Configuration { get; }

    public IPublisherBuilder<TOutput> ConfigurePublisher(string section)
    {
        _serviceCollection.Configure<PublisherOptions>(Configuration.GetSection(section));
        return this;
    }

    public IPublisherBuilder<TOutput> ConfigurePublisher(IConfiguration section)
    {
        _serviceCollection.Configure<PublisherOptions>(section);
        return this;
    }

    public void AddPublisher<TPublisher>()
        where TPublisher : IRawMessagePublisher<TOutput>
    {
        _serviceCollection.AddTransient(typeof(IRawMessagePublisher<TOutput>), typeof(TPublisher));
        _serviceCollection.AddTransient(typeof(TPublisher));
        _serviceCollection.AddTransient<IMessageEncoder, NoOpMessageEncoder>();
        _serviceCollection.AddSingleton<ITypedMessagePublisher<TOutput>, TypedMessagePublisher<TOutput, TPublisher>>();
    }

    public void AddPublisher<TPublisher>(Func<IServiceProvider, TPublisher> implementationFactory)
        where TPublisher : class, IRawMessagePublisher<TOutput>
    {
        _serviceCollection.AddTransient<IRawMessagePublisher<TOutput>>(implementationFactory);
        _serviceCollection.AddTransient(implementationFactory);
        _serviceCollection.AddTransient<IMessageEncoder, NoOpMessageEncoder>();
        _serviceCollection.AddSingleton<ITypedMessagePublisher<TOutput>, TypedMessagePublisher<TOutput, TPublisher>>();
    }

    public void AddSerializer<TSerializer>()
        where TSerializer : IMessageSerializer<TOutput> =>
        _serviceCollection.AddTransient(typeof(IMessageSerializer<TOutput>), typeof(TSerializer));

    public void AddEncoder<TEncoder>()
        where TEncoder : IMessageEncoder =>
        _serviceCollection.Replace(ServiceDescriptor.Transient(typeof(IMessageEncoder), typeof(TEncoder)));

    public IEnumerator<ServiceDescriptor> GetEnumerator() => _serviceCollection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(ServiceDescriptor item) => _serviceCollection.Add(item);

    public void Clear() => _serviceCollection.Clear();

    public bool Contains(ServiceDescriptor item) => _serviceCollection.Contains(item);

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _serviceCollection.CopyTo(array, arrayIndex);

    public bool Remove(ServiceDescriptor item) => _serviceCollection.Remove(item);

    public int Count => _serviceCollection.Count;
    public bool IsReadOnly => _serviceCollection.IsReadOnly;

    public int IndexOf(ServiceDescriptor item) => _serviceCollection.IndexOf(item);

    public void Insert(int index, ServiceDescriptor item) => _serviceCollection.Insert(index, item);

    public void RemoveAt(int index) => _serviceCollection.RemoveAt(index);

    public ServiceDescriptor this[int index]
    {
        get => _serviceCollection[index];
        set => _serviceCollection[index] = value;
    }

    public void Build(Action<HostBuilderContext, IPublisherBuilder<TOutput>> action)
    {
        var context = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = Configuration,
            HostingEnvironment = HostingEnvironment,
        };

        action(context, this);
    }
}
