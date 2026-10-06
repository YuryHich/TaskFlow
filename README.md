# TaskFlow

Backend-система управления проектами и задачами (трекер для компании).

**Проект в активной разработке.** Сейчас это ASP.NET Core **монолит** с REST API, JWT, PostgreSQL, узким Redis-кэшем, SignalR, RabbitMQ + MassTransit 8 и React SPA (`web/`). Слои Domain / Application / Infrastructure / API уже заложены.

## Стек

- .NET 10, ASP.NET Core Web API
- EF Core + PostgreSQL (Docker + pgAdmin)
- Redis: cache-aside (`IDistributedCache`), в тестах — in-memory
- RabbitMQ + MassTransit **8.3.6** (не v9: коммерческая лицензия)
- Kafka (KRaft): topic `taskflow.events`, аналитика отдельно от доставки тостов
- SignalR: хаб `/hubs/notifications` в процессе Notification (`:5032`); контракт `Notify` прежний
- React + TypeScript + Vite SPA (`web/`), TanStack Query
- JWT Bearer (HS256): реализация в Infrastructure, use cases в Application
- `IPasswordHasher<User>`, FluentValidation, Mapster
- Глобальный `IExceptionHandler` + ProblemDetails
- Swagger UI в Development (`/swagger`)
- Интеграционные тесты: `WebApplicationFactory` + xUnit, отдельная БД `TaskFlowDb_Tests`

## Локальный запуск

1. Инфраструктура: `docker compose up -d` из корня репозитория  
   Postgres `5432`, pgAdmin `http://localhost:5050`, Redis `6379`, Redis Insight `http://localhost:5540`,  
   RabbitMQ AMQP `5672`, Management UI `http://localhost:15672` (логин `taskflow`, пароль как `RABBITMQ_DEFAULT_PASS` в compose, по умолчанию `password`),  
   Kafka `localhost:9092` (топик `taskflow.events`, 3 partition; внутри сети брокера — `taskflow-kafka:19092`). Логи брокера не вынесены в volume: образ пишет их от пользователя без прав на Docker volume, поэтому пересоздание контейнера стирает топик, а `taskflow-kafka-init` создаёт его заново. Логи брокера живут внутри контейнера: том не смонтирован, потому что образ `apache/kafka` пишет их от пользователя без прав на Docker volume.  
   В Insight хост Redis — `taskflow-redis`, порт `6379` (не `localhost`).
2. Секреты API (Development, User Secrets, не коммитятся):

```text
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<строка к Postgres>" --project API
dotnet user-secrets set "Jwt:Key" "<строка не короче 32 символов>" --project API
dotnet user-secrets set "RabbitMQ:Password" "<тот же пароль, что RABBITMQ_DEFAULT_PASS>" --project API
```

`RabbitMQ:Password` должен совпадать с паролем брокера, иначе MassTransit получит `ACCESS_REFUSED`. Host/user/vhost — в `appsettings.json`.

Строка Redis по умолчанию в `appsettings.json`: `localhost:6379,abortConnect=false,connectTimeout=1000`. API стартует и без Redis (кэш miss, REST жив). Без RabbitMQ в Development шина не поднимется (REST может слушать порт, realtime и audit — нет). Без Kafka REST тоже жив: publish в топик логируется и пропускается.

Вторая база `TaskFlowAnalytics` создаётся скриптом `docker/init-extra-dbs.sql`, но **только на пустом** volume Postgres. Если `taskflow_pgdata` уже существует:

```text
docker exec -it taskflow-postgres psql -U postgres -c "CREATE DATABASE \"TaskFlowAnalytics\";"
```

3. Процессы (три `dotnet run`, секреты `Jwt:Key` и `RabbitMQ:Password` общие — у Analytics и Notification тот же User Secrets id, что у API):

```text
dotnet run --project API --launch-profile http            → http://localhost:5031
dotnet run --project Analytics --launch-profile http      → http://localhost:5040
dotnet run --project Notification --launch-profile http   → http://localhost:5032
```

Swagger API: `http://localhost:5031/swagger`. В Development хаб на API не слушает (`/hubs` на `:5031` → 404). Тосты идут в Notification. Audit остаётся consumer'ом внутри API.

4. Фронт: `npm install` (один раз) и `npm run dev` в `web/` → `http://localhost:5173`  
   Vite: `/api/analytics` → `:5040`, остальные `/api` → `:5031`, `/hubs` → `:5032` (WebSocket). Пустой `VITE_API_URL` — same-origin через proxy. Не задавайте `VITE_API_URL` на прямой порт API: аналитика и хаб обойдут прокси. CORS разрешает прямой origin `http://localhost:5173`, если proxy не используете.

   Если путь репозитория содержит `#` (например `D:\C#\...`), `npm run dev` поднимает Vite через junction без `#` (`resolve.preserveSymlinks`). Встроенный браузер Cursor может отдавать 404 на `/@vite/client` — откройте тот же URL в обычном Chrome или соберите превью: `npm run build` и `npm run preview` (`http://localhost:4173`).

Access token в памяти, refresh в `sessionStorage`. Через ~14 минут клиент сам обновляет access (`POST /api/auth/refresh`, ротация). F5 восстанавливает сессию. Два пользователя — два окна/инкогнито.

Миграции уже в репозитории. Если база пустая:

```text
dotnet ef database update --project Infrastructure --startup-project API
dotnet user-secrets set "ConnectionStrings:Analytics" "Host=localhost;Port=5432;Database=TaskFlowAnalytics;Username=postgres;Password=password" --project API
dotnet ef database update --project Analytics --startup-project Analytics
```

Секрет `ConnectionStrings:Analytics` пишется в тот же User Secrets store, что и API (`--project API` или `--project Analytics` — id один).

Тесты (нужен поднятый Postgres из compose; Redis, RabbitMQ и Kafka **не** нужны: в `Testing` кэш in-memory, MassTransit InMemory, Kafka publisher — no-op, хаб SignalR in-process в API; dev-база `TaskFlowDB` не используется):

```text
dotnet test API.Tests/API.Tests.csproj
dotnet test Analytics.Tests/Analytics.Tests.csproj
```

Скрипты Windows: `scripts/start-apps.cmd` (Chrome, Cursor, VS Code, Spotify, WireGuard, Docker Desktop), `scripts/start-taskflow.cmd` (compose + API + Vite + окна логина и админок).

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

После `SaveChanges` сервисы публикуют события через `IAppEventPublisher`. Ошибка подписчика не откатывает HTTP-ответ. Кэш по-прежнему in-process; SignalR и audit — через шину (Sprint 6).

**Кэш (узкий, cache-aside):** ключи только `project:{id}`, `task:{id}`, `tags:all`. TTL: 5 / 2 / 30 минут (`Cache:*Minutes`). Списки не кэшируются. Source of truth — Postgres. Hit не обходит 403. Инвалидация — `CacheInvalidationHandler` на те же события (теги — `TagCatalogChangedEvent`). Если Redis недоступен — warning и miss, REST работает.

**SignalR:** хаб `[Authorize]` `/hubs/notifications`. Подключение: заголовок `Authorization: Bearer` или query `?access_token=` (только путь `/hubs`). Клиент слушает метод `Notify`, тело `{ eventName, projectId, taskId, commentId }`. С Sprint 6 `Notify` уходит из `NotificationConsumer`, не из HTTP-пайплайна.

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

### Sprint 6 — RabbitMQ + MassTransit (event-driven monolith)

Один процесс API. Application не ссылается на MassTransit: сервисы по-прежнему вызывают `IAppEventPublisher`.

**In-process:** инвалидация Redis (`CacheInvalidationHandler`), каталог тегов (`TagCatalogChangedEvent` в шину не публикуется).

**Шина (project / task / comment):** после успешного сохранения `BusBridgeHandler` делает `Publish`. Две очереди (fan-out, не competing consumers):

- `taskflow-notifications` → процесс Notification (`:5032`) → SignalR. В `Testing` тот же consumer остаётся внутри API, чтобы `dotnet test` не требовал второй хост
- `taskflow-audit` → `AuditConsumer` в API → таблица `AuditLogs` (unique `EventId`)

Контракт хаба не менялся. В Development аудитория считается в API **до** publish (`AudienceUserIds` на каждом событии); Notification только рассылает `Clients.Users` и в Postgres не ходит.

**Dual-write:** Postgres уже закоммичен, затем независимо Rabbit и Kafka. Если брокер недоступен, HTTP-ответ всё равно успешен. Outbox не внедрялся.

### Sprint 7–8 — Kafka, Analytics, Notification

`KafkaBridgeHandler` пишет те же 9 событий (не теги) в topic `taskflow.events`. Key: `projectId` для проектов, `taskId` для задач и комментариев. Envelope: `eventId`, `eventType` (`task.created` и т.д.), `occurredAt`, `version`, `payload`.

Analytics (`:5040`) — отдельный процесс и база `TaskFlowAnalytics`. Consumer group `taskflow-analytics`, commit offset после `SaveChanges`. Факты по `TaskId` / `ProjectId`, не счётчики `++`. Повтор `EventId` не меняет цифры. `GET /api/analytics/summary` и `/projects` — только Admin/Manager. Страница `/analytics` в SPA (staff). Комментарии в топике пропускаются.

Notification (`:5032`) слушает только очередь `taskflow-notifications`. В Development API эту очередь не потребляет (иначе competing consumers).

MassTransit **8.3.6** (Apache 2.0). `GET /api/audit?take=50` — только Admin/Manager. Пароль брокера — User Secrets `RabbitMQ:Password`, тот же, что у контейнера. В `Testing` живой RabbitMQ и Kafka для `dotnet test` не нужны.

```text
React --/api--> API :5031 --Rabbit--> Notification :5032 --SignalR--> React
                 |     \\--Rabbit--> AuditLogs (тот же API)
                 |     \\--Kafka--> Analytics :5040 --> TaskFlowAnalytics
                 +--> TaskFlowDB, Redis
```

## Что будет дальше

CRUD (users / projects / tasks) остаётся одним процессом. Отдельные сервисы — только side effect и read model.

Уже сделано в Sprint 7–8: Kafka + Analytics (`:5040`, база `TaskFlowAnalytics`) и Notification (`:5032`, без своей БД). Audit по-прежнему в API. Нет gRPC, gateway, outbox и разрезания CRUD. Dockerfile'ы процессов не входят в эти спринты: API, Analytics и Notification запускаются через `dotnet run`.

### Replay аналитики

Цифры eventual consistent. Проверка догона:

1. Остановить Analytics, создать задачи через API, запустить Analytics снова — consumer group продолжит с сохранённого offset.
2. Пересобрать с начала: остановить Analytics, в `TaskFlowAnalytics` выполнить `TRUNCATE "ProcessedEvents", "TaskFacts", "ProjectFacts";`, в `Analytics/appsettings.json` выставить `Kafka:ResetToBeginning` = `true`, запустить, дождаться цифр, вернуть флаг в `false`. Иначе каждый старт будет читать топик с нуля (идемпотентность не задвоит факты, но сделает старт долгим).

Подробный разбор файлов, флоу и теория — в `отчёт по спринтам 7-8.md`.
