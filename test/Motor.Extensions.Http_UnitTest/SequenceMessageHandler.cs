using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Http_UnitTest;

public class SequenceMessageHandler : DelegatingHandler
{
    public int CallCount { get; private set; }

    public List<Func<HttpRequestMessage, HttpResponseMessage>> Responses { get; } =
        new List<Func<HttpRequestMessage, HttpResponseMessage>>();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken token)
    {
        var func = Responses[CallCount++ % Responses.Count];
        return Task.FromResult(func(request));
    }
}
