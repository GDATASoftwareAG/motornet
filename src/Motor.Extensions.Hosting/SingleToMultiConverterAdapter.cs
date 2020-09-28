using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting
{
    public class SingleToMultiConverterAdapter<TInput, TOutput> : IMultiResultMessageConverter<TInput, TOutput>
        where TInput : class 
        where TOutput : class
    {
        private readonly IMessageConverter<TInput, TOutput> _converter;

        public SingleToMultiConverterAdapter(IMessageConverter<TInput, TOutput> converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }
        
        public async Task<IEnumerable<MotorCloudEvent<TOutput>>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent, CancellationToken token)
        {
            var convertMessage = await _converter.ConvertMessageAsync(dataCloudEvent, token)
                .ConfigureAwait(false);
            return convertMessage == null ? new MotorCloudEvent<TOutput>[0] : new[] {convertMessage};
        }
    }
}
