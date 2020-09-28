using System.Collections.Generic;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Internal
{
    public class BaseDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
        where TInput : class
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public BaseDelegatingMessageHandler(PrepareDelegatingMessageHandler<TInput> prepare,
            IMessageHandler<TInput> messageHandler,
            IEnumerable<DelegatingMessageHandler<TInput>> delegatingMessageHandlers)
        {
            DelegatingMessageHandler<TInput> currentDelegatingMessageHandler = this;
            foreach (var delegatingMessageHandler in delegatingMessageHandlers)
            {
                currentDelegatingMessageHandler.InnerMessageHandler = delegatingMessageHandler;
                currentDelegatingMessageHandler = delegatingMessageHandler;
            }

            prepare.InnerMessageHandler = messageHandler;
            currentDelegatingMessageHandler.InnerMessageHandler = prepare;
        }
    }
}
