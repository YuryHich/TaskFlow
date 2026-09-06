# TaskFlow

Backend-система управления проектами и задачами (трекер для компании).

**Проект в активной разработке.** Сейчас это ASP.NET Core монолит с REST API и PostgreSQL. Слои Domain / Application / Infrastructure / API уже заложены. Ниже — что уже есть и что ещё планируется; если смотрите репозиторий «в середине пути», ожидайте незавершённые части (нет JWT, кэша, брокеров и микросервисов).

## Стек сейчас

- .NET 10, ASP.NET Core Web API
- EF Core + PostgreSQL (Docker + pgAdmin)
- FluentValidation, Mapster
- Слои: сущности и контракты репозиториев в Domain, сервисы и DTO в Application, EF и инфраструктура в Infrastructure

Локальный запуск БД: `docker compose up -d` из корня репозитория. API: `dotnet run --project API` (строка подключения — User Secrets в Development).

## Что уже сделано (Sprint 1)

- Модель: User, Project, Task, Comment, Tag и связи между ними
- REST CRUD, DTO, валидация
- Сервисы и репозитории (EF Core)
- PostgreSQL, миграции, просмотр данных в pgAdmin

## Что будет дальше

### Аутентификация и доступ

- Регистрация и вход
- JWT (access / refresh), хеширование паролей
- Роли, claims, policies
- Закрытые эндпоинты и разграничение прав

### CQRS и тесты

- CQRS и MediatR (команды и запросы)
- Unit- и интеграционные тесты

### Redis и SignalR

- Кэш часто читаемых данных (cache-aside, инвалидация)
- Real-time обновления и уведомления при изменениях задач и проектов

### События и очереди

- Domain / integration events
- RabbitMQ (producer / consumer, exchanges, очереди)
- MassTransit
- Асинхронные уведомления и обработка событий

### Микросервисы и gRPC

- Выделение bounded contexts: User, Task, Notification и другие
- gRPC между сервисами
- REST как внешний API (gateway)

### Kafka и аналитика

- Публикация событий в Kafka
- Отдельный Analytics Service: статистика по задачам, пользователям и проектам

### Инфраструктура

- Dockerfile сервисов, Compose на всю систему
- PostgreSQL, Redis, RabbitMQ, Kafka и сервисы в контейнерах
- Настройки через переменные окружения, health checks, базовый logging / monitoring

### Целевая схема

```text
                         Client
                           │
                           ▼
                      API / Gateway
                           │
             ┌─────────────┼─────────────┐
             ▼             ▼             ▼
        User Service   Task Service   Project Service
             │             │             │
             └─────────────┼─────────────┘
                           │
                         gRPC
                           │
                    ┌──────┴──────┐
                    ▼             ▼
                RabbitMQ        Redis
                    │
             ┌──────┴──────┐
             ▼             ▼
       Notification     Audit Service
          Service

                    Kafka
                      │
                      ▼
               Analytics Service
                      │
                  PostgreSQL
```

