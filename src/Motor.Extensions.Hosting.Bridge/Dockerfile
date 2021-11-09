FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
COPY . /data
WORKDIR /data

ARG DOTNET_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=$DOTNET_ENVIRONMENT

HEALTHCHECK CMD wget --quiet --tries=1 --spider http://localhost:9110/health || exit 1

ENTRYPOINT dotnet /data/Motor.Extensions.Hosting.Bridge.dll

LABEL org.opencontainers.image.title="Motor.NET Bridge Docker Image" \
      org.opencontainers.image.description="Motor.NET" \
      org.opencontainers.image.url="https://github.com/GDATASoftwareAG/motornet" \
      org.opencontainers.image.source="https://github.com/GDATASoftwareAG/motornet/tree/master/src/Motor.Extensions.Hosting.Bridge/" \
      org.opencontainers.image.license="MIT"
