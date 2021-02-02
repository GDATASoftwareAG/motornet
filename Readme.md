![Motor.NET Logo](./rsc/Icon_Motor_NET_128_Typo.png "Motor.NET - Drives your microservices")

[![GitHub](https://img.shields.io/github/license/GDATASoftwareAG/motornet)](https://raw.githubusercontent.com/GDATASoftwareAG/motornet/master/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/GDATASoftwareAG/motornet/.NET%20Core)](https://github.com/GDATASoftwareAG/motornet/actions)
[![Nuget](https://img.shields.io/nuget/v/Motor.Extensions.Hosting)](https://www.nuget.org/packages/Motor.Extensions.Hosting/)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/f74a6fde2c2d490bb60f42590d554e1c)](https://www.codacy.com/gh/GDATASoftwareAG/motornet/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=GDATASoftwareAG/motornet&amp;utm_campaign=Badge_Grade)

## About Motor.NET
Motor.NET is a micro-service framework for .NET built on top of [Microsoft Generic Hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1). It provides easy integration of RabbitMQ, Kafka (WIP) and HTTP as well as helpers for logging and tracing.

You should be up and running with just a few lines of code.

## Examples

You find working examples for different use-cases under the [examples](./examples) folder.

- [Consume and publish to RabbitMQ](./examples/ConsumeAndPublishWithRabbitMQ)
- [Consume and publish to Kafka](./examples/ConsumeAndPublishWithKafka)
- [Consume and publish multiple messages at once to RabbitMQ](./examples/ConsumeAndMultiOutputPublishWithRabbitMQ)
- [Create a service with metrics](./examples/MetricsExample)
- [Create a service with custom traces](./examples/OpenTelemetryExample)

## Support Matrix

| Component | Consume | Publish | CloudEvents | Metrics | Custom |
| --- | --- | --- | --- | --- | --- |
| RabbitMQ | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | priority, dynamic routing |
| Kafka | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | partitioning key, dynamic topic |
| Http | (:heavy_check_mark:) | (:heavy_check_mark:) | :x: |:heavy_check_mark:| |
| Timer | (:heavy_check_mark:) | - | :x: | :x:| |
| SQS | (:heavy_check_mark:) | - | :x: | :x:| |

## License

Motor.NET is provided under the [MIT](./LICENSE) license.
