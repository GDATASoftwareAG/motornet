# Motor.NET

[![GitHub](https://img.shields.io/github/license/GDATASoftwareAG/motornet)](https://raw.githubusercontent.com/GDATASoftwareAG/motornet/master/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/GDATASoftwareAG/motornet/.NET%20Core)](https://github.com/GDATASoftwareAG/motornet/actions)
[![Nuget](https://img.shields.io/nuget/v/Motor.Extensions.Hosting)](https://www.nuget.org/packages/Motor.Extensions.Hosting/)

Motor.NET is a micro-service framework for .NET built on top of [Microsoft Generic Hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1). It provides easy integration of RabbitMQ, Kafka (WIP) and HTTP as well as helpers for logging and tracing.

You should be up and running with just a few lines of code.

## Examples

You find working examples for different use-cases under the [examples](./examples) folder.

- [Consume and publish to RabbitMQ](./examples/ConsumeAndPublishWithRabbitMQ)
- [Create a service with metrics](./examples/Metrics)

## Support Matrix

| Component | Consume | Publish | CloudEvents | Metrics | Custom |
| --- | --- | --- | --- | --- | --- |
| RabbitMQ | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | priority, dynamic routing |
| Kafka | :heavy_check_mark: | :x: | :x: |:heavy_check_mark:| |
| Http | (:heavy_check_mark:) | (:heavy_check_mark:) | :x: |:heavy_check_mark:| |

## License

Motor.NET is provided under the [MIT](./LICENSE) license.
