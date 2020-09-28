using System;
using System.Linq.Expressions;
using Moq;
using Polly;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
    public static class MockExtensions
    {
        public static void VerifyUntilTimeoutAsync<T>(this Mock<T> mock,Expression<Action<T>> expression, Func<Times> times, int retries = 4)
            where T : class
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(retries, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) 
                )
                .Execute(() => { mock.Verify(expression, times); });
        }
    }
}