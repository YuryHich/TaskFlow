# TaskFlow

Backend-система управления проектами и задачами (трекер для компании).

**Проект в активной разработке.** Сейчас это ASP.NET Core монолит с REST API, JWT, PostgreSQL, узким Redis-кэшем, SignalR и React SPA (`web/`). Слои Domain / Application / Infrastructure / API уже заложены.

## Стек

- .NET 10, ASP.NET Core Web API
- EF Core + PostgreSQL (Docker + pgAdmin)
- Redis: cache-aside (`IDistributedCache`), в тестах — in-memory
- SignalR: хаб уведомлений `/hubs/notifications`, JWT
- React + TypeScript + Vite SPA (`web/`), TanStack Query
- JWT Bearer (HS256): реализация в Infrastructure, use cases в Application
- `IPasswordHasher<User>`, FluentValidation, Mapster
- Глобальный `IExceptionHandler` + ProblemDetails
- Swagger UI в Development (`/swagger`)
- Интеграционные тесты: `WebApplicationFactory` + xUnit, отдельная БД `TaskFlowDb_Tests`

## Локальный запуск

1. Инфраструктура: `docker compose up -d` из корня репозитория  
   Postgres `5432`, pgAdmin `5050`, Redis `6379`, Redis Insight `http://localhost:5540`  
   В Insight хост Redis — `taskflow-redis`, порт `6379` (не `localhost`).
2. Секреты API (Development, User Secrets, не коммитятся):

```text
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<строка к Postgres>" --project API
dotnet user-secrets set "Jwt:Key" "<строка не короче 32 символов>" --project API
```

Строка Redis по умолчанию в `appsettings.json`: `localhost:6379,abortConnect=false,connectTimeout=1000`. API стартует и без Redis (кэш miss, REST жив).

3. API: `dotnet run --project API --launch-profile http` → `http://localhost:5031`  
   Swagger: `http://localhost:5031/swagger`

4. Фронт: `npm install` (один раз) и `npm run dev` в `web/` → `http://localhost:5173`  
   Vite проксирует `/api` и `/hubs` на API (WebSocket включён). Пустой `VITE_API_URL` в `.env.example` — same-origin через proxy. CORS на API разрешает прямой origin `http://localhost:5173`, если proxy не используете.

   Если путь репозитория содержит `#` (например `D:\C#\...`), `npm run dev` поднимает Vite через junction без `#` (`resolve.preserveSymlinks`). Встроенный браузер Cursor может отдавать 404 на `/@vite/client` — откройте тот же URL в обычном Chrome или соберите превью: `npm run build` и `npm run preview` (`http://localhost:4173`).

Access token в памяти, refresh в `sessionStorage`. Через ~14 минут клиент сам обновляет access (`POST /api/auth/refresh`, ротация). F5 восстанавливает сессию. Два пользователя — два окна/инкогнито.

Миграции уже в репозитории. Если база пустая:  
`dotnet ef database update --project Infrastructure --startup-project API`

Тесты (нужен поднятый Postgres из compose; Redis **не** нужен — в `Testing` кэш in-memory; dev-база `TaskFlowDB` не используется):

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
- Публичного `POST /api/users` нет — пользователи только через register

### Sprint 3 — тесты, слои, owner, чтение, ошибки

- Интеграционные тесты API: auth (register/login/refresh rotation и reuse, logout), 401 без токена, права на проекты, назначение owner
- `JwtService` в Infrastructure; Application зависит только от порта `IJwtService`
- Owner: Admin/Manager назначают существующего пользователя при создании или через PUT
- Списки projects / tasks / comments фильтруются в SQL, не после `ToList`
- Единый exception handler (ProblemDetails): 400 / 401 / 403 / 404 / 409
- FluentValidation через action filter, контроллеры без ручного `ValidateAsync`
- CQRS / MediatR не вводили: сервисы по use case достаточны для текущего CRUD

### Sprint 4 — аудитория проекта, Redis, SignalR

Чтение проекта (и всех его задач / комментариев), если **Admin | Manager, или owner, или assignee любой задачи этого проекта**. Явной таблицы участников нет: аудитория собирается как owner ∪ исполнители задач ∪ Admin/Manager.

На задаче несколько исполнителей: `AssigneeIds[]` (create/update заменяет набор целиком). Мутации проекта и задач — по-прежнему owner или Admin/Manager. Assignee задачу не редактирует.

После `SaveChanges` сервисы публикуют in-process события (`IAppEventPublisher`). Ошибка подписчика не откатывает HTTP-ответ.

**Кэш (узкий, cache-aside):** ключи только `project:{id}`, `task:{id}`, `tags:all`. TTL: 5 / 2 / 30 минут (`Cache:*Minutes`). Списки не кэшируются. Source of truth — Postgres. Hit не обходит 403. Инвалидация — `CacheInvalidationHandler` на те же события (теги — `TagCatalogChangedEvent`). Если Redis недоступен — warning и miss, REST работает.

**SignalR:** хаб `[Authorize]` `/hubs/notifications`. Подключение: заголовок `Authorization: Bearer` или query `?access_token=` (только путь `/hubs`). Клиент слушает метод `Notify`, тело `{ eventName, projectId, taskId, commentId }`.

Имена: `project.created|updated|deleted`, `task.created|updated|deleted`, `comment.added|updated|deleted`. Теги в хаб не уходят. Доставка `Clients.Users(audience)`, не groups и не `All`. На delete проекта/задачи аудитория считается **до** удаления. Access 15 минут: клиент сам делает refresh и открывает новое соединение (reconnect на бэке нет).

Кратко по правам:

| | Developer | Admin / Manager |
|---|---|---|
| Проекты list / GET | где он owner **или** assignee любой задачи | все |
| Создать / удалить проект | нет | да |
| Назначить owner | нет | да: `OwnerId` в POST или PUT (пользователь должен существовать) |
| Проект PUT (имя, описание) | только свой как owner | любой |
| Сменить `OwnerId` | нет (даже если owner) | да |
| Задачи создать / PUT / DELETE | только в своём проекте (owner) | все |
| Задачи GET (включая соседние в проекте) | owner **или** assignee любой задачи проекта | все |
| Комментарии | доступ к задаче (TaskAccess = чтение проекта) | все |
| Теги GET | любой залогиненный | то же |
| Теги запись | нет | да |
| Users `/me`, `/directory` | да (id, email, username) | то же |
| Users GET список | нет | да |
| Users PUT | только себя | Admin — любого; Manager — себя |
| Users DELETE | нет | только Admin |

Чужая существующая сущность → **403**, нет записи → **404**.  
`POST /api/projects` без `OwnerId` — владелец = текущий Admin/Manager.

### Sprint 5 — React SPA

Клиент в `web/`: логин / регистрация / logout, проекты, задачи (`AssigneeIds`), комментарии, каталог тегов, профиль, админка пользователей (staff). Кнопки по роли JWT и `ownerId`. 403 с бэка показывается как «нет прав».

Живые обновления: одно соединение на `/hubs/notifications`, метод `Notify` инвалидирует TanStack Query. Теги с хаба не приходят. После `project.deleted` открытая карточка уходит на список.

Добор API для UI: CORS (`http://localhost:5173`), `GET /api/users/me`, `GET /api/users/directory`. `GET /api/users` по-прежнему только Admin/Manager.

## Что будет дальше

### Sprint 6 — события и очереди

- Вынести in-process события во внешние очереди
- RabbitMQ (producer / consumer, exchanges, очереди)
- MassTransit

### Микросервисы и gRPC

- Выделение bounded contexts: User, Task, Notification и другие
- gRPC между сервисами
- REST как внешний API (gateway)

### Kafka и аналитика

- Публикация событий в Kafka
- Отдельный Analytics Service: статистика по задачам, пользователям и проектам

### Инфраструктура

- Dockerfile сервисов, Compose на всю систему (сейчас в compose только Postgres, pgAdmin, Redis, Insight; API — `dotnet run`)
- RabbitMQ, Kafka и сервисы в контейнерах
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
