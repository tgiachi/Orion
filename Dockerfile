# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/OrionIrcd.Core/OrionIrcd.Core.csproj src/OrionIrcd.Core/
COPY src/OrionIrcd.IRC/OrionIrcd.IRC.csproj src/OrionIrcd.IRC/
COPY src/OrionIrcd.Network/OrionIrcd.Network.csproj src/OrionIrcd.Network/
COPY src/OrionIrcd.Server/OrionIrcd.Server.csproj src/OrionIrcd.Server/

RUN dotnet restore src/OrionIrcd.Server/OrionIrcd.Server.csproj

COPY src/ src/

RUN dotnet publish src/OrionIrcd.Server/OrionIrcd.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0 \
    HOME=/app

RUN mkdir -p /data && chown -R app:app /app /data

USER app

COPY --from=build --chown=app:app /app/publish ./

VOLUME ["/data"]
EXPOSE 6666 6667 6668

ENTRYPOINT ["dotnet", "OrionIrcd.Server.dll", "--root-directory", "/data"]
