# Тестовое задание .Net - Сервис расчета расстояний между аэропортами

## Описание
REST-сервис для получения расстояния в милях между двумя аэропортами. Аэропорты определяются трёхбуквенным кодом IATA.

## Технологии
- .NET 8.0
- PostgreSQL
- Docker
- Принципы чистой архитектуры и SOLID

## Запуск проекта

### 1. Запуск базы данных PostgreSQL в Docker

```
docker-compose up -d
```

### 2. Запуск API

1. Откройте решение в Visual Studio или Rider
2. Запустите проект `FlyDistance.Api`

### 3. Данные можно загрузить из файла **iata-icao** и потом тестить 

## API Эндпоинты

### 1. Получение информации об аэропорте

```
GET /api/airports/{code}
```

### 2. Расчет расстояния между аэропортами

```
GET /api/distance?from={from}&to={to}
```

### 3. Импорт данных из CSV-файла

```
POST /api/import/csv
```

## Архитектура

Проект реализован с использованием принципов чистой архитектуры:

- **FluDistance.Domain** — доменные модели и интерфейсы
- **FlyDistance.Application** — бизнес-логика приложения
- **FluDistance.Infrastructure** — реализации репозиториев и внешние интеграции
- **FlyDistance.Api** — API endpoints (Minimal API)

## Оптимизация производительности

Для повышения производительности и отказоустойчивости реализовано кэширование:

**Кэширование расстояний между аэропортами** - однажды вычисленные расстояния сохраняются и повторно используются

Это позволяет:
- Снизить количество вычеслений
- Ускорить ответ сервиса
