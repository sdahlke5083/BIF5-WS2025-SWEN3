# Unter https://aka.ms/customizecontainer erfahren Sie, wie Sie Ihren Debugcontainer anpassen und wie Visual Studio dieses Dockerfile verwendet, um Ihre Images für ein schnelleres Debuggen zu erstellen.

# Diese Stufe wird verwendet, wenn sie von VS im Schnellmodus ausgeführt wird (Standardeinstellung für Debugkonfiguration).
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Default RabbitMQ environment variables (can be overridden by docker-compose or runtime)
ENV RABBITMQ_HOST=paperless-rabbitmq \
    RABBITMQ_PORT=5672 \
    RABBITMQ_USER=paperless \
    RABBITMQ_PASS=paperless \
    RABBITMQ_EXCHANGE=tasks \
    RABBITMQ_EXCHANGE_TYPE=direct

# Default Postgres env (can be overridden in docker-compose)
ENV POSTGRES_HOST=paperless-db \
    POSTGRES_PORT=5432 \
    POSTGRES_DB=paperless \
    POSTGRES_USER=paperless \
    POSTGRES_PASSWORD=paperless

# MinIO defaults (can be overridden)
ENV MINIO_ROOT_USER=minioadmin \
    MINIO_ROOT_PASSWORD=minioadmin \
    MINIO_BUCKET=paperless \
    MINIO_ENDPOINT=paperless-minio:9000 \
    MINIO_USE_SSL=false

# Elasticsearch defaults
ENV ELASTICSEARCH_HOST=paperless-elasticsearch \
    ELASTICSEARCH_PORT=9200

# JWT defaults (please override in production using secure secrets)
ENV Jwt__Key=please_change_this_secret_in_production \
    Jwt__Issuer=paperless.local \
    Jwt__Audience=paperless.local

# Ensure ASP.NET Core listens on port 8081 inside container (matches docker-compose)
ENV ASPNETCORE_URLS=http://+:8081


# Diese Stufe wird zum Erstellen des Dienstprojekts verwendet.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Paperless.REST.csproj", "./"]
RUN dotnet restore "./Paperless.REST.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "./Paperless.REST.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Diese Stufe wird verwendet, um das Dienstprojekt zu veröffentlichen, das in die letzte Phase kopiert werden soll.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Paperless.REST.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Diese Stufe wird in der Produktion oder bei Ausführung von VS im regulären Modus verwendet (Standard, wenn die Debugkonfiguration nicht verwendet wird).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Paperless.REST.dll"]