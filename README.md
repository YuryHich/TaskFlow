# TaskFlow

Backend-система управления проектами и задачами (трекер для компании).

**Проект в активной разработке.** Сейчас это ASP.NET Core монолит с REST API и PostgreSQL. Слои Domain / Application / Infrastructure / API уже заложены.

## Стек

- .NET 10, ASP.NET Core Web API
- EF Core + PostgreSQL (Docker + pgAdmin)
- JWT Bearer (HS256): реализация в Infrastructure, use cases в Application
- `IPasswordHasher<User>`, FluentValidation, Mapster
- Глобальный `IExceptionHandler` + ProblemDetails
- Swagger UI в Development (`/swagger`)
- Интеграционные тесты: `WebApplicationFactory` + xUnit, отдельная БД `TaskFlowDb_Tests`

## Локальный запуск

1. БД: `docker compose up -d` из корня репозитория (Postgres `5432`, pgAdmin `5050`).
2. Секреты API (Development, User Secrets, не коммитятся):

```text
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<строка к Postgres>" --project API
dotnet user-secrets set "Jwt:Key" "<строка не короче 32 символов>" --project API
```

3. API: `dotnet run --project API --launch-profile http` → `http://localhost:5031`  
   Swagger: `http://localhost:5031/swagger`

Миграции уже в репозитории. Если база пустая:  
`dotnet ef database update --project Infrastructure --startup-project API`

Тесты (нужен поднятый Postgres из compose; dev-база `TaskFlowDB` не используется):

```text
dotnet test API.Tests/API.Tests.csproj
```

## Что уже сделано

### Sprint 1 — доменная модель и CRUD

- Сущности: User, Project, WorkTask, Comment, Tag и связи
- REST CRUD, DTO, валидация
- Сервисы и EF-репозитории
- PostgreSQL, миграции, pgAdmin

### Sprint 2 — аутентификация и авторизация

- Регистрация и вход: username + email + пароль, роль при register всегда **Developer**
- JWT: access 15 минут, refresh 7 дней (в БД только hash), ротация, reuse → отзыв всех refresh пользователя, logout отзывает refresh
- Эндпоинты: `POST /api/auth/register` (201), `/login`, `/refresh`, `/logout`
- Пароль: минимум 8 символов, верхний и нижний регистр, цифра
- Все остальные API требуют `Authorization: Bearer <accessToken>` (без токена — 401)
- Роли Admin / Manager / Developer. Роль через API не меняется: для тестов выставляете в БД (`Users.Role`), затем снова login
- Доступ без участников проекта: owner проекта, assignee задачи, Admin/Manager
- Публичного `POST /api/users` нет — пользователи только через register

Кратко по правам:

| | Developer | Admin / Manager |
|---|---|---|
| Проекты list | только где он `OwnerId` | все |
| Создать / удалить проект | нет | да |
| Назначить owner | нет | да: `OwnerId` в POST или PUT (пользователь должен существовать) |
| Проект GET / PUT (имя, описание) | только свой как owner | любой |
| Сменить `OwnerId` | нет (даже если owner) | да |
| Задачи создать / PUT / DELETE | только в своём проекте (owner) | все |
| Задачи GET | assignee **или** owner проекта | все |
| Комментарии | доступ к задаче (TaskAccess) | все |
| Теги GET | любой залогиненный | то же |
| Теги запись | нет | да |
| Users GET | нет | да |
| Users PUT | только себя | Admin — любого; Manager — себя |
| Users DELETE | нет | только Admin |

Чужая существующая сущность → **403**, нет записи → **404**.  
`POST /api/projects` без `OwnerId` — владелец = текущий Admin/Manager. Assignee задачи проект в списке не видит и задачу не редактирует (только GET и комментарии).

### Sprint 3 — тесты, слои, owner, чтение, ошибки

- Интеграционные тесты API: auth (register/login/refresh rotation и reuse, logout), 401 без токена, права на проекты, назначение owner
- `JwtService` в Infrastructure; Application зависит только от порта `IJwtService`
- Owner: Admin/Manager назначают существующего пользователя при создании или через PUT
- Списки projects / tasks / comments фильтруются в SQL, не после `ToList`
- Единый exception handler (ProblemDetails): 400 / 401 / 403 / 404 / 409
- FluentValidation через action filter, контроллеры без ручного `ValidateAsync`
- CQRS / MediatR не вводили: сервисы по use case достаточны для текущего CRUD

## Что будет дальше

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
