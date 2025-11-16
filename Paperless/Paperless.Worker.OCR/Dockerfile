# Unter https://aka.ms/customizecontainer erfahren Sie, wie Sie Ihren Debugcontainer anpassen und wie Visual Studio dieses Dockerfile verwendet, um Ihre Images für ein schnelleres Debuggen zu erstellen.

# Diese Stufe wird verwendet, wenn sie von VS im Schnellmodus ausgeführt wird (Standardeinstellung für Debugkonfiguration).
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
# Install dependencies
RUN apt-get update && apt-get install -y tesseract-ocr tesseract-ocr-eng libtesseract-dev && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app


# Diese Stufe wird zum Erstellen des Dienstprojekts verwendet.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Paperless.Worker.OCR/Paperless.Worker.OCR.csproj", "Paperless.Worker.OCR/"]
RUN dotnet restore "./Paperless.Worker.OCR/Paperless.Worker.OCR.csproj"
COPY . .
WORKDIR "/src/Paperless.Worker.OCR"
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