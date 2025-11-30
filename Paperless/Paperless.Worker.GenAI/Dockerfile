# Basis: .NET 9 Runtime für den fertigen Container
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

# Default RabbitMQ environment variables (can be overridden by docker-compose or runtime)
ENV RABBITMQ_HOST=paperless-rabbitmq \
    RABBITMQ_PORT=5672 \
    RABBITMQ_USER=paperless \
    RABBITMQ_PASS=paperless \
    RABBITMQ_EXCHANGE=tasks \
    RABBITMQ_EXCHANGE_TYPE=direct \
    RABBITMQ_QUEUE=genai-queue

# Build-Stage: SDK-Image zum Kompilieren
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ganzen Paperless-Solution-Ordner in das Build-Image kopieren
COPY . .

# Restore & Build nur für den GenAI-Worker
RUN dotnet restore "./Paperless.Worker.GenAI/Paperless.Worker.GenAI.csproj"
WORKDIR "/src/Paperless.Worker.GenAI"
RUN dotnet build "./Paperless.Worker.GenAI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish-Stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Paperless.Worker.GenAI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Finaler Runtime-Container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Paperless.Worker.GenAI.dll"]
