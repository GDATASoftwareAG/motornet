using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting
{
    public class DefaultApplicationNameService : IApplicationNameService
    {
        private readonly Assembly _assembly;

        public DefaultApplicationNameService(Assembly assembly)
        {
            _assembly = assembly;
        }

        public IEnumerable<string> ToRemoveEndings { get; set; } = new List<string> { "Service", "Console" };

        public IDictionary<string, string> ToReplace { get; set; } = new Dictionary<string, string>
        {
            {".", ""},
            {"_", ""}
        };

        public string GetProduct()
        {
            var assemblyProductAttribute =
                (AssemblyProductAttribute?)Attribute.GetCustomAttribute(_assembly, typeof(AssemblyProductAttribute));
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
            return Assembly.GetAssembly(typeof(DefaultApplicationNameService))?.GetName()?.Version?.ToString() ?? "debug";
        }

        public string GetFullName()
        {
            return ExtractServiceName(GetProduct(), GetAssemblyName());
        }

        public Uri GetSource()
        {
            return new Uri($"motor://{GetFullName()}");
        }

        public string GetAssemblyName()
        {
            return _assembly.GetName().Name!;
        }

        private static string PascalCaseToKebabCase(string project)
        {
            return string.Concat(project.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString()));
        }

        public string ExtractServiceName(string product, string assembly)
        {
            var project = ToReplace
                .Aggregate(assembly, (current, replaces) => current.Replace(replaces.Key, replaces.Value));

            foreach (var service in ToRemoveEndings.Where(t => project.EndsWith(t)))
            {
                project = project.Substring(0, project.Length - service.Length);
            }

            project = PascalCaseToKebabCase(project);
            return $"{product}/{project}".ToLower();
        }
    }
}
