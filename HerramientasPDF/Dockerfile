# Esta fase se usa cuando se ejecuta desde VS en modo rápido
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# --- CONFIGURACIÓN INSTITUCIONAL DE MOTORES (PDF Y OFFICE) ---
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    ghostscript \
    libreoffice-writer \
    libreoffice-calc \
    libreoffice-impress \
    libfreetype6 \
    libfontconfig1 \
    libxrender1 \
    libxt6 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Aseguramos permisos para el binario de Ghostscript
RUN chmod +x /usr/bin/gs
# --- FIN DE CONFIGURACIÓN MANUAL ---

# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HerramientasPDF/HerramientasPDF.csproj", "HerramientasPDF/"]
RUN dotnet restore "./HerramientasPDF/HerramientasPDF.csproj"
COPY . .
WORKDIR "/src/HerramientasPDF"
RUN dotnet build "./HerramientasPDF.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HerramientasPDF.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o modo normal
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
ENTRYPOINT ["dotnet", "HerramientasPDF.dll"]