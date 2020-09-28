using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities;
using OpenTracing;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Xunit;

namespace Motor.Extensions.Http_UnitTest
{
    [Collection("GenericHosting")]
    public class TracingDelegatingHandlerTests
    {
        [Fact]
        public async Task GetAsync_RequestFailureAndOkResponse_RequestExceptionIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => throw new HttpRequestException(),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.GetAsync("https://dot.net");

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("GET", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.False(finishedSpans[0].Tags.ContainsKey(Tags.HttpStatus.Key));
            Assert.Equal(true, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task GetAsync_InternalServerErrorAndOkResponse_InternalServerErrorIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.GetAsync("https://dot.net");

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("GET", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.Equal(500, finishedSpans[0].Tags[Tags.HttpStatus.Key]);
            Assert.Equal(true, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task GetAsync_OkResponse_OkResponseIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.GetAsync("https://dot.net");

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("GET", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.Equal(200, finishedSpans[0].Tags[Tags.HttpStatus.Key]);
            Assert.Equal(false, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task GetAsync_RequestFailureAndOkResponse_PipelineTested()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => throw new HttpRequestException(),
                    req => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.GetAsync("https://dot.net");

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal(3, finishedSpans.Count);
            Assert.Equal(3, handler.CallCount);
        }

        [Fact]
        public async Task GetAsync_ParentSpan_TracesHasParentRef()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            using (tracer.BuildSpan("test").StartActive())
            {
                await httpClient.GetAsync("https://dot.net");
            }

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal(finishedSpans[1].Context.SpanId, finishedSpans[0].ParentId);
        }

        [Fact]
        public async Task PostAsync_RequestFailureAndOkResponse_RequestExceptionIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => throw new HttpRequestException(),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.PostAsync("https://dot.net", new StringContent(""));

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("POST", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.False(finishedSpans[0].Tags.ContainsKey(Tags.HttpStatus.Key));
            Assert.Equal(true, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task PostAsync_InternalServerErrorAndOkResponse_InternalServerErrorIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.PostAsync("https://dot.net", new StringContent(""));

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("POST", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.Equal(500, finishedSpans[0].Tags[Tags.HttpStatus.Key]);
            Assert.Equal(true, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task PostAsync_OkResponse_OkResponseIsTraced()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.PostAsync("https://dot.net", new StringContent(""));

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal("POST", finishedSpans[0].Tags[Tags.HttpMethod.Key]);
            Assert.Equal(200, finishedSpans[0].Tags[Tags.HttpStatus.Key]);
            Assert.Equal(false, finishedSpans[0].Tags[Tags.Error.Key]);
        }

        [Fact]
        public async Task PostAsync_RequestFailureAndInternalServerErrorAndOkResponse_PipelineTested()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => throw new HttpRequestException(),
                    req => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            await httpClient.PostAsync("https://dot.net", new StringContent(""));

            await Task.Delay(TimeSpan.FromMinutes(1));

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal(3, finishedSpans.Count);
            Assert.Equal(3, handler.CallCount);
        }

        [Fact]
        public async Task PostAsync_ParentSpan_TracesHasParentRef()
        {
            var handler = new SequenceMessageHandler
            {
                Responses =
                {
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                }
            };
            var tracer = new MockTracer();
            var httpClient = CreateHttpClient(tracer, handler);

            using (tracer.BuildSpan("test").StartActive())
            {
                await httpClient.PostAsync("https://dot.net", new StringContent(""));
            }

            var finishedSpans = tracer.FinishedSpans();
            Assert.Equal(finishedSpans[1].Context.SpanId, finishedSpans[0].ParentId);
        }

        private static HttpClient CreateHttpClient(ITracer tracer, SequenceMessageHandler handler)
        {
            var hostBuilder = new MotorHostBuilder(new HostBuilder())
                .ConfigurePrometheus()
                .ConfigureDefaultHttpClient()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(provider =>
                    {
                        var mock = new Mock<IApplicationNameService>();
                        mock.Setup(t => t.GetVersion()).Returns("test");
                        mock.Setup(t => t.GetLibVersion()).Returns("test");
                        return mock.Object;
                    });
                    services.AddSingleton(provider => tracer);
                    var addHttpClient = services.AddDefaultHttpClient("test");
                    addHttpClient.ConfigureHttpMessageHandlerBuilder(t => { t.PrimaryHandler = handler; });
                });

            var httpClient = hostBuilder.Build()
                .Services.GetService<IHttpClientFactory>()
                .CreateClient("test");
            return httpClient;
        }
    }
}
