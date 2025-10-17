![Motor.NET Logo](./rsc/Icon_Motor_NET_128_Typo.png "Motor.NET - Drives your microservices")

[![GitHub](https://img.shields.io/github/license/GDATASoftwareAG/motornet)](https://raw.githubusercontent.com/GDATASoftwareAG/motornet/master/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/GDATASoftwareAG/motornet/dotnet.yml?branch=master)](https://github.com/GDATASoftwareAG/motornet/actions)
[![Nuget](https://img.shields.io/nuget/v/Motor.Extensions.Hosting)](https://www.nuget.org/packages/Motor.Extensions.Hosting/)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/f74a6fde2c2d490bb60f42590d554e1c)](https://www.codacy.com/gh/GDATASoftwareAG/motornet/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=GDATASoftwareAG/motornet&amp;utm_campaign=Badge_Grade)

## About Motor.NET

Motor.NET is a micro-service framework for .NET built on top of [Microsoft Generic Hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1).
It provides easy integration of RabbitMQ, Kafka (WIP) and HTTP as well as helpers for logging and tracing.

You should be up and running with just a few lines of code.

## Examples

You find working examples for different use-cases under the [examples](./examples) folder.

- [Consume and publish to RabbitMQ](./examples/ConsumeAndPublishWithRabbitMQ)
- [Consume from RabbitMQ with Dead Letter Exchange](./examples/ConsumeWithRabbitMQAndDeadLetterExchange)
- [Consume and publish to Kafka](./examples/ConsumeAndPublishWithKafka)
- [Consume and publish multiple messages at once to RabbitMQ](./examples/ConsumeAndMultiOutputPublisherWithRabbitMQ)
- [Create a service with metrics](./examples/MetricsExample)
- [Create a service with custom traces](./examples/OpenTelemetryExample)
- [Integration tests for message handling services](./examples/ConsumeAndPublishWithKafka_IntegrationTest)
- [Create](../examples/MvcExample) and [test](../examples/MvcExample__IntegrationTest) a backend API with AspNetCore MVC

## Support Matrix

| Component | Consume                | Publish              | CloudEvents (Protocol) | CloudEvents (JSON) | Metrics            | Compression        | Custom                          |
|-----------|------------------------|----------------------|------------------------|--------------------|--------------------|--------------------|---------------------------------|
| RabbitMQ  | :heavy_check_mark:     | :heavy_check_mark:   | :heavy_check_mark:     | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | priority, dynamic routing       |
| Kafka     | :heavy_check_mark:     | :heavy_check_mark:   | :heavy_check_mark:     | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | partitioning key, dynamic topic |
| Timer     | ( :heavy_check_mark: ) | -                    | :x:                    | :x:                | :x:                | :x:                |                                 |
| SQS       | ( :heavy_check_mark: ) | -                    | :x:                    | :x:                | :x:                | :x:                |                                 |
| NATS      | ( :heavy_check_mark: ) | :heavy_check_mark:   | :x:                    | :heavy_check_mark: | :x:                | :x:                |                                 |

**CloudEvents (Protocol)**: If supported, the protocol format uses headers from Kafka or RabbitMQ to store CloudEvent metadata.

**CloudEvents (JSON)**: If supported, CloudEvent published in the enveloped variant, see https://github.com/cloudevents/spec/blob/v1.0.1/json-format.md#3-envelope. 

## Health Checks

Motor.NET comes by default already with two health checks for message processing services (RabbitMQ, Kafka, Timer, and SQS):

- `MessageProcessingHealthCheck`: Fails when no messages were consumed in a certain time frame from the Motor.NET internal queue although it has at least some messages.
- `TooManyTemporaryFailuresHealthCheck`: Fails when too many messages led to a failure since the last message was correctly handled (either successful or as invalid input).

## Compression (Optional)

[Gzip][gzip] compression can optionally be enabled for consumers and publishers. By enabling it for a publisher, the payload of
all published messages will be compressed. Enabling it for a consumer will allow the consumer to decompress these
messages. Consumers can however still consume uncompressed messages. This should make it easy to enable compression in
an existing environment that does not use compression yet. It just needs to be enabled first for the consumers and
afterwards for the publishers.

## Thread Starvation Handling

Since .NET 6, `Environment.ProcessorCount` can be overwritten using env `DOTNET_PROCESSOR_COUNT`, see [issue](https://github.com/dotnet/runtime/issues/48094) and [blog](https://devblogs.microsoft.com/dotnet/announcing-net-6/#optimizing-scaling). This can be particularly helpful within container runtimes with cpu resources being limited by cgroups.

## License

Motor.NET is provided under the [MIT](./LICENSE) license.

[gzip]: https://www.gnu.org/software/gzip/
