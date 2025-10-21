using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting;

public class DefaultApplicationNameService : IApplicationNameService
{
    private readonly Assembly _assembly;
    private readonly DefaultApplicationNameOptions _options;

    public DefaultApplicationNameService(Assembly assembly, IOptions<DefaultApplicationNameOptions> options)
    {
        _assembly = assembly;
        _options = options.Value ?? new DefaultApplicationNameOptions();
    }

    private IEnumerable<string> ToRemoveEndings { get; set; } = new List<string> { "Service", "Console" };

    private IDictionary<string, string> ToReplace { get; set; } =
        new Dictionary<string, string> { { ".", "" }, { "_", "" } };

    private string GetProduct()
    {
        var assemblyProductAttribute = (AssemblyProductAttribute?)
            Attribute.GetCustomAttribute(_assembly, typeof(AssemblyProductAttribute));
        if (assemblyProductAttribute?.Product == GetAssemblyName())
        {
            throw new InvalidProgramException("Product is not set.");
        }

        return assemblyProductAttribute!.Product;
    }

    public string GetVersion()
    {
        return _assembly.GetName().Version?.ToString() ?? "debug";
    }

    public string GetLibVersion()
    {
        return Assembly.GetAssembly(typeof(DefaultApplicationNameService))?.GetName().Version?.ToString() ?? "debug";
    }

    public string GetFullName()
    {
        if (string.IsNullOrWhiteSpace(_options.FullName))
        {
            return ExtractServiceName(GetProduct(), GetAssemblyName());
        }

        return _options.FullName.Trim();
    }

    public Uri GetSource()
    {
        if (string.IsNullOrWhiteSpace(_options.Source))
        {
            return new($"motor://{GetFullName()}");
        }

        return new(_options.Source.Trim());
    }

    private string GetAssemblyName()
    {
        return _assembly.GetName().Name!;
    }

    private static string PascalCaseToKebabCase(string project)
    {
        return string.Concat(project.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString()));
    }

    public string ExtractServiceName(string product, string assembly)
    {
        var project = ToReplace.Aggregate(
            assembly,
            (current, replaces) => current.Replace(replaces.Key, replaces.Value)
        );

        foreach (var service in ToRemoveEndings.Where(t => project.EndsWith(t)))
        {
            project = project.Substring(0, project.Length - service.Length);
        }

        project = PascalCaseToKebabCase(project);
        return $"{product}/{project}".ToLower();
    }
}
