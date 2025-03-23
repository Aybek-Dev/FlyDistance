FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Создаем пользователя для запуска без прав суперпользователя
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FlyDistance.Api/FlyDistance.Api.csproj", "FlyDistance.Api/"]
COPY ["FlyDistance.Application/FlyDistance.Application.csproj", "FlyDistance.Application/"]
COPY ["FluDistance.Domain/FluDistance.Domain.csproj", "FluDistance.Domain/"]
COPY ["FluDistance.Infrastructure/FluDistance.Infrastructure.csproj", "FluDistance.Infrastructure/"]
RUN dotnet restore "FlyDistance.Api/FlyDistance.Api.csproj"
COPY . .
WORKDIR "/src/FlyDistance.Api"
RUN dotnet build "FlyDistance.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlyDistance.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Копируем CSV файл в контейнер
COPY iata-icao.csv /app/iata-icao.csv

# Делаем CSV-файл доступным пользователю приложения
USER root
RUN chown appuser:appuser /app/iata-icao.csv
USER appuser

ENTRYPOINT ["dotnet", "FlyDistance.Api.dll"] 