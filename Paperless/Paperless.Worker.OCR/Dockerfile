# Unter https://aka.ms/customizecontainer erfahren Sie, wie Sie Ihren Debugcontainer anpassen und wie Visual Studio dieses Dockerfile verwendet, um Ihre Images für ein schnelleres Debuggen zu erstellen.

# Diese Stufe wird verwendet, wenn sie von VS im Schnellmodus ausgeführt wird (Standardeinstellung für Debugkonfiguration).
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
    RABBITMQ_QUEUE=ocr-queue

# Install dependencies
USER root
RUN apt-get update && \
	apt-get install -y tesseract-ocr \
	  libtesseract-dev \
      libleptonica-dev && \
	rm -rf /var/lib/apt/lists/*

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

WORKDIR /app/x64

RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so
#RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /app/x64/libtesseract50.so

# Clean up to reduce image size
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
# Hack to Allow Tesseract to work

# Switch back to the non-root user
USER $APP_UID


# Diese Stufe wird zum Erstellen des Dienstprojekts verwendet.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# copy the project file from the project folder (build context is the project directory)
COPY ["Paperless.Worker.OCR.csproj", "./"]
RUN dotnet restore "./Paperless.Worker.OCR.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "./Paperless.Worker.OCR.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Diese Stufe wird verwendet, um das Dienstprojekt zu veröffentlichen, das in die letzte Phase kopiert werden soll.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Paperless.Worker.OCR.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Diese Stufe wird in der Produktion oder bei Ausführung von VS im regulären Modus verwendet (Standard, wenn die Debugkonfiguration nicht verwendet wird).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Paperless.Worker.OCR.dll"]