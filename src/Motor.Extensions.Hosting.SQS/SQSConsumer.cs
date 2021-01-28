using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS
{
    public class SQSConsumer<TData> : IMessageConsumer<TData>, IDisposable where TData : notnull
    {
        private readonly IAmazonSQS _sqs;
        private readonly SQSConsumerOptions<TData> _options;
        private readonly ILogger<SQSConsumer<TData>> _logger;
        private readonly IApplicationNameService _applicationNameService;
        private readonly ISQSClientFactory _sqsClientFactory;

        public SQSConsumer(IAmazonSQS sqs, IOptions<SQSConsumerOptions<TData>> options, ILogger<SQSConsumer<TData>> logger,
            IApplicationNameService applicationNameService, ISQSClientFactory sqsClientFactory)
        {

            _sqs = new AmazonSQSClient();
            _sqs = sqs;
            _options = options.Value;
            _logger = logger;
            _applicationNameService = applicationNameService;
            _sqsClientFactory = sqsClientFactory;
        }

        public async Task ExecuteAsync(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _options.SqsUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 5
                    };

                    var result = await _sqs.ReceiveMessageAsync(request, token);
                    if (result.Messages.Any())
                    {
                        foreach (var message in result.Messages)
                        {
                            SingleMessageHandling(message, token);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No messages received");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    if (e.InnerException != null) 
                        _logger.LogError(e.InnerException.Message);
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
        }

        private void SingleMessageHandling(Message message, CancellationToken token)
        {
            var dataCloudEvent = message.Body.ToMotorCloudEvent(_applicationNameService);
            var taskAwaiter = ConsumeCallbackAsync?.Invoke(dataCloudEvent, token).GetAwaiter();
            taskAwaiter?.OnCompleted(async () =>
            {
                var processedMessageStatus = taskAwaiter?.GetResult();
                switch (processedMessageStatus)
                {
                    case ProcessedMessageStatus.Success:
                        break;
                    case ProcessedMessageStatus.TemporaryFailure:
                        break;
                    case ProcessedMessageStatus.InvalidInput:
                        break;
                    case ProcessedMessageStatus.CriticalFailure:
                        break;
                    default:
                         throw new ArgumentOutOfRangeException(nameof(processedMessageStatus),
                             processedMessageStatus.ToString());
                }
                await DeleteMessageAsync(message, token);
            });
        }

        private async Task DeleteMessageAsync(Message message, CancellationToken token)
        {
            var deleteResult =
                await _sqs.DeleteMessageAsync(_options.SqsUrl, message.ReceiptHandle, token);
            if (deleteResult.HttpStatusCode != HttpStatusCode.OK)
                _logger.LogDebug("Could not delete message");
        }

        public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
        {
            get;
            set;
        }

        public Task StartAsync(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
