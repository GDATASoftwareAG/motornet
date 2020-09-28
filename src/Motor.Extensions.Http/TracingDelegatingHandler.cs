using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Motor.Extensions.Http
{
    internal class TracingDelegatingHandler : DelegatingHandler
    {
        private readonly ITracer _tracer;

        public TracingDelegatingHandler(ITracer tracer)
        {
            _tracer = tracer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using (var scope = _tracer.BuildSpan($"{request.Method} - {request.RequestUri.Host}")
                .WithTag(Tags.HttpUrl, request.RequestUri.AbsoluteUri)
                .WithTag(Tags.HttpMethod, request.Method.Method)
                .StartActive())
            {
                var headers = new Dictionary<string, string>();
                _tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(headers));
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }

                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    scope.Span
                        .SetTag(Tags.HttpStatus, (int) response.StatusCode)
                        .SetTag(Tags.Error, !response.IsSuccessStatusCode);

                    return response;
                }
                catch(HttpRequestException)
                {
                    scope.Span.SetTag(Tags.Error, true);
                    throw;
                }
            }
        }
    }
}
