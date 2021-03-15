FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
COPY . /data
WORKDIR /data

ARG DOTNET_ENVIRONMENT
ENV DOTNET_ENVIRONMENT=$DOTNET_ENVIRONMENT

HEALTHCHECK CMD wget --quiet --tries=1 --spider http://localhost:9110/health || exit 1

ENTRYPOINT dotnet /data/Motor.Extensions.Hosting.Shifter.dll
