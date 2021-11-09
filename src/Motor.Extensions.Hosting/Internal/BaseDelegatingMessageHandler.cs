using System.Collections.Generic;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Internal;

public class BaseDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
    where TInput : class
{
    // ReSharper disable once SuggestBaseTypeForParameter
    public BaseDelegatingMessageHandler(PrepareDelegatingMessageHandler<TInput> prepare,
        INoOutputService<TInput> service,
        IEnumerable<DelegatingMessageHandler<TInput>> delegatingMessageHandlers)
    {
        DelegatingMessageHandler<TInput> currentDelegatingMessageHandler = this;
        foreach (var delegatingMessageHandler in delegatingMessageHandlers)
        {
            currentDelegatingMessageHandler.InnerService = delegatingMessageHandler;
            currentDelegatingMessageHandler = delegatingMessageHandler;
        }

        prepare.InnerService = service;
        currentDelegatingMessageHandler.InnerService = prepare;
    }
}
