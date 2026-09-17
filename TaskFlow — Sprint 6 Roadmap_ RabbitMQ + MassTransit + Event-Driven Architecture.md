# ⚡ TaskFlow — Sprint 6 Roadmap

## RabbitMQ + MassTransit + Event-Driven Architecture

**Текущий этап:** после Sprint 5  
**Фокус:** переход от in-process событий к асинхронному взаимодействию через RabbitMQ.

---

# 🎯 Цель спринта

В предыдущем спринте TaskFlow использовал:

```text
Service
   │
   ├── SaveChanges()
   │
   └── IAppEventPublisher
           │
           ├── CacheInvalidationHandler
           └── SignalR notification handler
```

Это работает внутри одного процесса.

В Sprint 6 необходимо перейти к:

```text
Service
   │
   ├── PostgreSQL
   │
   └── Event Publisher
          │
          ▼
      RabbitMQ
          │
     ┌────┴─────────────┐
     ▼                  ▼
Consumer A          Consumer B
     │                  │
     ▼                  ▼
Notification       Audit / Cache /
Service             Other handler
```

Главная идея:

> **HTTP-запрос больше не обязан напрямую выполнять всю последующую работу.**

Сервис сохраняет данные → публикует событие → RabbitMQ доставляет событие подписчикам → каждый consumer независимо выполняет свою работу.

---

# 🧠 Что должно измениться после Sprint 6

До:

```text
POST /api/tasks
       │
       ▼
TaskService
       │
       ├── PostgreSQL
       │
       ├── Cache invalidation
       │
       └── SignalR notification
```

После:

```text
POST /api/tasks
       │
       ▼
TaskService
       │
       ├── PostgreSQL
       │
       └── Publish TaskCreatedEvent
                    │
                    ▼
                RabbitMQ
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
 Notification Consumer    Audit Consumer
          │                   │
          ▼                   ▼
       SignalR             Audit log
```

В дальнейшем этот же механизм можно будет использовать для:

- Notification Service
- Audit Service
- Analytics Service
- User Service
- Task Service
- Project Service
- интеграции с Kafka
- других consumers

---

# 0. 🗺️ Теория перед началом

Перед написанием кода необходимо понимать следующие понятия.

## 0.1. Synchronous vs asynchronous

Синхронное взаимодействие:

```text
A → B → C → response
```

A ждёт B.

Асинхронное:

```text
A → Message Broker
        │
        ├──→ B
        └──→ C

A → response
```

A не обязан ждать выполнения B и C.

---

# 0.2. Event

Event — факт, который уже произошёл.

Например:

```text
TaskCreated
TaskUpdated
TaskDeleted
CommentAdded
ProjectCreated
ProjectDeleted
```

Хорошее название события отвечает на вопрос:

> Что произошло?

Например:

```csharp
TaskCreatedEvent
```

а не:

```csharp
CreateTaskCommand
```

Это разные концепции.

---

# 0.3. Event vs Command

### Event

```text
TaskCreatedEvent
```

означает:

> Задача уже была создана.

Event обычно публикуется для одного или нескольких заинтересованных consumers.

### Command

```text
CreateTaskCommand
```

означает:

> Сделай создание задачи.

Command обычно адресуется конкретному обработчику.

В этом спринте основной фокус:

```text
Events
```

CQRS/MediatR не возвращаем.

---

# 0.4. Message Broker

RabbitMQ выступает посредником:

```text
Producer
   │
   ▼
RabbitMQ
   │
   ▼
Consumer
```

Producer не обязан знать:

- где находится consumer;
- сколько consumers существует;
- когда именно consumer обработает сообщение;
- какая реализация consumer используется.

---

# 0.5. Queue

Queue — очередь сообщений.

Пример:

```text
notification-queue

┌──────────────────────────┐
│ TaskCreated              │
│ TaskAssigned             │
│ CommentAdded             │
│ TaskStatusChanged        │
└──────────────────────────┘
```

Consumer читает сообщения из queue.

---

# 0.6. Exchange

Producer обычно отправляет сообщение не непосредственно в queue.

Упрощённо:

```text
Producer
   ↓
Exchange
   ↓
Queue
   ↓
Consumer
```

Exchange отвечает за маршрутизацию сообщений.

Важно понимать:

```text
Exchange ≠ Queue
```

---

# 0.7. Routing

RabbitMQ может направлять сообщения в разные queues.

Например:

```text
                 TaskCreated
                     │
                     ▼
              task-events exchange
                /             \
               /               \
              ▼                 ▼
     notification-queue     audit-queue
              │                 │
              ▼                 ▼
        Notification        Audit
```

Один event может быть обработан несколькими независимыми consumers.

---

# 0.8. Delivery

В messaging нельзя исходить из предположения:

> Сообщение обработается ровно один раз.

На практике необходимо учитывать:

```text
at-most-once
at-least-once
exactly-once
```

Для TaskFlow основной практический подход:

```text
at-least-once delivery
+
idempotent consumers
```

То есть consumer должен нормально переживать повторную доставку одного события.

---

# 0.9. Retry

Consumer может временно упасть:

```text
RabbitMQ
   ↓
Consumer
   ↓
DB unavailable
```

Вместо окончательной потери сообщения можно сделать:

```text
retry
retry
retry
dead-letter
```

На этом этапе необходимо понять:

- retry;
- redelivery;
- delayed retry;
- dead-letter;
- fault.

MassTransit значительно упростит эту работу.

---

# 0.10. Dead Letter / Fault

Если consumer не смог обработать сообщение после всех попыток:

```text
Message
   ↓
Consumer
   ↓
FAIL
   ↓
Retry
   ↓
FAIL
   ↓
Retry
   ↓
FAIL
   ↓
Dead Letter / Fault
```

Это необходимо для диагностики проблем.

---

# 1. 🐇 RabbitMQ в Docker Compose

## Цель

Добавить RabbitMQ в существующую инфраструктуру.

Сейчас:

```text
PostgreSQL
pgAdmin
Redis
Redis Insight
```

Должно стать:

```text
PostgreSQL
pgAdmin
Redis
Redis Insight
RabbitMQ
RabbitMQ Management
```

---

# 1.1. RabbitMQ Management UI

Использовать официальный образ RabbitMQ с management plugin.

После запуска должна быть доступна management-панель.

Необходимо научиться смотреть:

- exchanges;
- queues;
- consumers;
- connections;
- messages;
- bindings;
- rates;
- message states.

---

# 1.2. Docker network

Убедиться, что:

```text
API
PostgreSQL
Redis
RabbitMQ
```

находятся в одной Docker-сети, если API будет запускаться контейнером.

При локальном запуске API:

```text
localhost
```

может использоваться для подключения к RabbitMQ.

При запуске API внутри Docker:

```text
rabbitmq
```

должен использоваться как hostname.

Это важный момент:

```text
localhost
```

внутри контейнера означает:

> этот же контейнер

а не RabbitMQ.

---

# 1.3. Проверка RabbitMQ

После запуска:

```text
docker compose up -d
```

проверить:

```text
docker compose ps
```

и:

```text
docker compose logs rabbitmq
```

Необходимо убедиться, что RabbitMQ:

- запущен;
- принимает подключения;
- management UI работает.

---

# 2. 📦 Добавление MassTransit

## Цель

Не работать с RabbitMQ API напрямую во всех сервисах.

Архитектура:

```text
Application
     │
     ▼
Messaging abstraction
     │
     ▼
MassTransit
     │
     ▼
RabbitMQ
```

MassTransit будет отвечать за:

- connection;
- publishing;
- consuming;
- serialization;
- endpoint configuration;
- retry;
- consumer lifecycle;
- middleware;
- routing.

---

# 2.1. Почему не писать всё через RabbitMQ.Client

Важно понимать разницу.

Можно напрямую работать с:

```text
RabbitMQ.Client
```

Но тогда самостоятельно придётся решать множество задач:

- serialization;
- consumer lifecycle;
- retry;
- acknowledgements;
- endpoint configuration;
- routing;
- error handling;
- concurrency;
- integration patterns.

MassTransit предоставляет более высокий уровень абстракции.

---

# 2.2. Конфигурация

В `API` необходимо добавить MassTransit.

Конфигурация должна получать настройки из:

```text
appsettings.json
```

а секреты — через:

```text
User Secrets
```

или environment variables.

Не коммитить реальные credentials.

---

# 2.3. Connection settings

Сделать конфигурацию примерно такого типа:

```text
RabbitMQ:
  Host
  Port
  Username
  Password
  VirtualHost
```

Не хардкодить credentials в коде.

---

# 3. 🧱 Проектирование Event Contracts

Это один из самых важных этапов спринта.

События должны быть независимы от EF Core entities.

Нельзя:

```csharp
Publish(taskEntity);
```

Вместо этого:

```csharp
Publish(new TaskCreatedEvent(...));
```

---

# 3.1. TaskCreatedEvent

Создать contract:

```text
TaskCreatedEvent
```

Минимальная информация:

```text
EventId
OccurredAt
TaskId
ProjectId
Title
AssigneeIds
CreatedByUserId
```

---

# 3.2. TaskUpdatedEvent

Например:

```text
EventId
OccurredAt
TaskId
ProjectId
UpdatedByUserId
```

Если понадобится, добавить информацию об изменённых полях.

Но не тащить в event весь database entity.

---

# 3.3. TaskDeletedEvent

Важно:

```text
TaskId
ProjectId
DeletedByUserId
OccurredAt
EventId
```

Особенно важен `ProjectId`, потому что после удаления Task entity уже может не существовать.

---

# 3.4. Project events

Создать:

```text
ProjectCreatedEvent
ProjectUpdatedEvent
ProjectDeletedEvent
```

---

# 3.5. Comment events

Создать:

```text
CommentAddedEvent
CommentUpdatedEvent
CommentDeletedEvent
```

---

# 3.6. Event metadata

У каждого event желательно иметь:

```text
EventId
OccurredAt
```

Почему?

Для:

- диагностики;
- логирования;
- трассировки;
- идемпотентности;
- анализа последовательности событий.

---

# 3.7. Event contracts ≠ Domain entities

Нужно понимать:

```text
Domain Entity
```

и

```text
Integration Event
```

решают разные задачи.

Entity:

```text
Task
```

представляет состояние внутри домена.

Event:

```text
TaskCreatedEvent
```

сообщает внешним consumers:

> задача была создана.

---

# 4. 📤 Первый Producer

Начать с одного события.

Рекомендуемый вариант:

```text
TaskCreatedEvent
```

После создания задачи:

```text
TaskService
    │
    ├── SaveChanges()
    │
    └── Publish(TaskCreatedEvent)
```

---

# 4.1. Важный порядок

Событие должно публиковаться после успешного сохранения данных.

Неправильно:

```text
Publish event
SaveChanges
```

Потому что:

```text
event published
      ↓
SaveChanges failed
```

Получается:

> TaskCreated опубликован, хотя Task не существует.

Базовая версия:

```text
SaveChanges
   ↓
Publish event
```

---

# 4.2. Что если Publish упал?

Вот здесь необходимо понять важную проблему.

```text
PostgreSQL → SUCCESS
RabbitMQ   → FAIL
```

Получается:

```text
Task существует
Event потерян
```

И наоборот:

```text
RabbitMQ → SUCCESS
PostgreSQL → FAIL
```

может привести к:

```text
Event существует
Task отсутствует
```

Это проблема:

# Dual Write Problem

В этом спринте необходимо **понять эту проблему**, но не обязательно сразу внедрять Outbox Pattern.

Outbox будет логичным следующим этапом hardening/event architecture.

---

# 5. 📥 Первый Consumer

Создать consumer:

```text
TaskCreatedConsumer
```

Он должен получать:

```text
TaskCreatedEvent
```

и выполнить простое действие.

Например:

```text
TaskCreatedEvent
      ↓
Consumer
      ↓
логирование
```

Сначала **не надо сразу делать сложную бизнес-логику**.

Главная цель:

```text
Producer
   ↓
RabbitMQ
   ↓
Consumer
```

---

# 5.1. Проверка

Создать Task через:

```text
POST /api/tasks
```

После этого проверить:

```text
API logs
RabbitMQ Management
Consumer logs
```

Должно быть видно:

```text
TaskCreatedEvent published
TaskCreatedConsumer received
```

---

# 6. 🔀 Несколько Consumers

Теперь показать реальное преимущество event-driven архитектуры.

Для:

```text
TaskCreatedEvent
```

сделать минимум два consumer-направления:

```text
Notification Consumer
Audit Consumer
```

Архитектура:

```text
                 TaskCreatedEvent
                       │
                       ▼
                 RabbitMQ
                 /       \
                /         \
               ▼           ▼
       Notification      Audit
         Consumer        Consumer
```

Каждый consumer должен иметь собственную queue.

---

# 6.1. Почему разные queues

Если сделать:

```text
Queue
 ├── Consumer A
 └── Consumer B
```

то consumers могут конкурировать за одно сообщение.

Это не то же самое, что broadcast.

Для независимых обработчиков:

```text
notification-queue
audit-queue
```

оба получают событие.

---

# 7. 📣 Notification Consumer

Теперь перенести существующую notification-логику ближе к messaging architecture.

Текущая логика:

```text
Service
   ↓
IAppEventPublisher
   ↓
SignalR
```

Новая:

```text
Service
   ↓
RabbitMQ
   ↓
NotificationConsumer
   ↓
SignalR
```

---

# 7.1. Что делает Notification Consumer

Получает:

```text
TaskCreatedEvent
TaskUpdatedEvent
TaskDeletedEvent
CommentAddedEvent
...
```

и определяет:

```text
audience
```

После этого:

```text
Clients.Users(audience)
```

отправляется через SignalR.

---

# 7.2. Важное ограничение

Не переносить сразу всю существующую авторизационную логику в consumer.

В Sprint 4 аудитория была:

```text
Admin
Manager
Owner
Assignee
```

Необходимо сохранить существующую бизнес-логику доступа.

Consumer не должен внезапно отправлять событие всем пользователям.

---

# 8. 📝 Audit Consumer

Создать отдельный consumer для аудита.

Например:

```text
TaskCreated
TaskUpdated
TaskDeleted
ProjectCreated
ProjectUpdated
ProjectDeleted
```

Consumer записывает:

```text
EventId
EventType
OccurredAt
UserId
EntityId
```

---

# 8.1. Audit entity

Можно создать:

```text
AuditLog
```

например:

```text
Id
EventId
EventType
EntityType
EntityId
UserId
OccurredAt
Payload
```

Необходимо решить:

- хранить ли JSON payload;
- какие данные логировать;
- какие данные не логировать.

---

# 8.2. Почему Audit Service логически отдельный

Главный смысл:

```text
TaskService
```

не должен знать:

```text
как именно работает аудит
```

Он сообщает:

```text
TaskCreated
```

а дальше:

```text
Audit Consumer
```

сам решает:

```text
как сохранить событие
```

---

# 9. 🔄 Перенос остальных событий

После успешного первого сценария постепенно перенести:

### Projects

```text
ProjectCreatedEvent
ProjectUpdatedEvent
ProjectDeletedEvent
```

### Tasks

```text
TaskCreatedEvent
TaskUpdatedEvent
TaskDeletedEvent
```

### Comments

```text
CommentAddedEvent
CommentUpdatedEvent
CommentDeletedEvent
```

---

# 9.1. Сохранить существующую семантику

Особенно внимательно с delete.

Сейчас аудитория проекта/задачи вычисляется:

> **до удаления**

Это поведение нельзя случайно сломать.

Поэтому event должен содержать данные, которые после удаления уже невозможно получить.

Например:

```text
TaskDeletedEvent
{
    TaskId,
    ProjectId,
    Audience,
    DeletedByUserId,
    OccurredAt,
    EventId
}
```

Если audience вычисляется consumer-ом после удаления — данные могут быть уже недоступны.

---

# 10. 🧹 Что делать с IAppEventPublisher

После миграции на RabbitMQ необходимо постепенно отказаться от:

```text
IAppEventPublisher
```

как основного механизма доставки событий.

Было:

```text
IAppEventPublisher
       ↓
In-process handlers
```

Станет:

```text
MassTransit
       ↓
RabbitMQ
       ↓
Consumers
```

---

# 10.1. Но не удалять сразу

Лучше мигрировать постепенно.

Например:

```text
TaskCreated
```

перевести первым.

Проверить.

Затем:

```text
TaskUpdated
TaskDeleted
```

и далее.

---

# 11. 🧠 Producer abstraction

Не стоит делать Application полностью зависимым от MassTransit.

Хороший вариант:

```text
Application
    ↓
IEventPublisher
    ↓
Infrastructure
    ↓
MassTransit
    ↓
RabbitMQ
```

Например концептуально:

```csharp
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken);
}
```

Application знает:

> нужно опубликовать событие.

Но не знает:

> RabbitMQ это или Kafka.

Это особенно полезно перед будущим Kafka.

---

# 12. 🔁 Retry

Добавить retry для consumers.

Например концепция:

```text
Message
   ↓
Consumer
   ↓
Exception
   ↓
Retry
   ↓
Consumer
```

Настроить ограниченное количество попыток.

Например:

```text
3 retries
```

Не делать бесконечный retry.

---

# 12.1. Exponential backoff

Понять:

```text
1 sec
2 sec
4 sec
8 sec
```

вместо:

```text
1 sec
1 sec
1 sec
1 sec
```

Это снижает нагрузку при временной проблеме.

---

# 13. 💀 Fault / Error handling

Проверить ситуацию:

```text
Consumer
   ↓
throw exception
```

Посмотреть, что происходит с сообщением.

Изучить:

- error queue;
- fault message;
- retry;
- redelivery;
- failed consumers.

Главное — научиться диагностировать:

> почему событие не обработалось.

---

# 14. ♻️ Idempotency

Это обязательная теория Sprint 6.

Представим:

```text
TaskCreatedEvent
EventId = 123
```

Consumer обработал его.

Потом сообщение пришло ещё раз.

Если consumer создаёт запись:

```text
AuditLog
```

получим:

```text
AuditLog #1
AuditLog #2
```

хотя событие одно.

---

# 14.1. Идемпотентность

Consumer должен выдерживать повторное получение одного event.

Один из подходов:

```text
ProcessedMessage
```

с:

```text
EventId
ProcessedAt
```

Алгоритм:

```text
Receive Event
      ↓
EventId already processed?
      │
   ┌──┴──┐
  YES    NO
   │      │
 ignore   process
          │
          ▼
       save EventId
```

---

# 14.2. Уникальный индекс

Для:

```text
EventId
```

можно сделать:

```text
UNIQUE
```

чтобы база дополнительно защищала от дублей.

---

# 15. 🚦 Consumer concurrency

Изучить:

```text
Concurrent consumers
```

Например:

```text
Queue
│
├── Consumer instance 1
├── Consumer instance 2
└── Consumer instance 3
```

Сообщения могут обрабатываться параллельно.

Это значит:

> нельзя предполагать последовательную обработку всех событий.

Особенно важно для:

```text
TaskUpdated
TaskDeleted
```

---

# 16. ⏱️ Event ordering

Рассмотреть ситуацию:

```text
TaskUpdated
TaskDeleted
```

Но consumer получил:

```text
TaskDeleted
TaskUpdated
```

Что делать?

На этом этапе достаточно:

- понимать проблему;
- не предполагать глобальный порядок;
- использовать `OccurredAt`;
- использовать version/sequence при необходимости.

Не нужно пока строить сложную distributed ordering system.

---

# 17. 🧨 Cache Invalidation

В Sprint 4 кэш инвалидировался через:

```text
IAppEventPublisher
      ↓
CacheInvalidationHandler
```

Теперь:

```text
Task mutation
      ↓
RabbitMQ
      ↓
CacheInvalidationConsumer
      ↓
Redis
```

---

# 17.1. Проверить поведение

Например:

```text
GET /tasks/15
```

→ Redis HIT.

Потом:

```text
PUT /tasks/15
```

→ PostgreSQL update.

→ `TaskUpdatedEvent`.

→ RabbitMQ.

→ Cache consumer.

→:

```text
task:15 removed
```

Следующий:

```text
GET /tasks/15
```

должен идти:

```text
Redis MISS
   ↓
PostgreSQL
   ↓
Redis SET
```

---

# 17.2. Важная семантика

Redis остаётся:

```text
cache
```

а не:

```text
source of truth
```

То есть:

```text
RabbitMQ unavailable
```

не должен автоматически означать:

```text
PostgreSQL unavailable
```

---

# 18. 📡 SignalR через RabbitMQ

Итоговая схема notifications:

```text
Client
  │
  │ REST
  ▼
API
  │
  ▼
PostgreSQL
  │
  ▼
RabbitMQ
  │
  ▼
Notification Consumer
  │
  ▼
SignalR Hub
  │
  ▼
Clients.Users(...)
```

Это важное изменение архитектуры.

---

# 18.1. Почему это лучше текущей схемы

Теперь бизнес-сервис не должен напрямую знать о SignalR.

Было:

```text
TaskService → SignalR
```

Станет:

```text
TaskService → Event
```

А:

```text
Notification Consumer → SignalR
```

Таким образом SignalR становится инфраструктурой уведомлений.

---

# 19. 📊 Audit + Notification одновременно

Финальная схема для TaskCreated:

```text
                    ┌───────────────┐
                    │ TaskService   │
                    └───────┬───────┘
                            │
                     SaveChanges()
                            │
                            ▼
                     TaskCreatedEvent
                            │
                            ▼
                        RabbitMQ
                       /         \
                      /           \
                     ▼             ▼
          Notification Queue    Audit Queue
                  │                 │
                  ▼                 ▼
        NotificationConsumer   AuditConsumer
                  │                 │
                  ▼                 ▼
               SignalR          AuditLog DB
```

Это один из главных результатов Sprint 6.

---

# 20. 🔐 Security

RabbitMQ нельзя оставлять:

```text
guest / guest
```

как production configuration.

Изучить:

- username/password;
- virtual host;
- permissions;
- environment variables;
- secrets;
- management UI security.

Для локальной разработки допустима простая конфигурация, но credentials не должны попадать в Git.

---

# 21. ⚙️ Configuration

Все настройки RabbitMQ вынести в configuration:

```text
RabbitMQ:
    Host
    Port
    Username
    Password
    VirtualHost
```

Не делать:

```csharp
var password = "123456";
```

---

# 22. 🧪 Интеграционные тесты

Не ограничиваться unit tests.

Необходимо проверить messaging flow.

Минимальные сценарии:

### Test 1 — TaskCreated

```text
POST /tasks
      ↓
DB
      ↓
Event
      ↓
Consumer
```

---

### Test 2 — Notification

```text
TaskCreated
      ↓
RabbitMQ
      ↓
NotificationConsumer
      ↓
notification sent
```

---

### Test 3 — Audit

```text
TaskCreated
      ↓
RabbitMQ
      ↓
AuditConsumer
      ↓
AuditLog
```

---

### Test 4 — Retry

Искусственно сделать consumer, который падает.

Проверить:

```text
attempt 1
attempt 2
attempt 3
```

---

### Test 5 — Idempotency

Один event отправить дважды.

Проверить:

```text
AuditLog count = 1
```

---

### Test 6 — Redis invalidation

```text
cache exists
      ↓
TaskUpdated
      ↓
consumer
      ↓
cache removed
```

---

# 23. 🐳 Docker Compose

В конце спринта инфраструктура должна выглядеть примерно так:

```text
TaskFlow
│
├── PostgreSQL
├── pgAdmin
├── Redis
├── Redis Insight
└── RabbitMQ
     └── Management UI
```

API пока можно оставить запускаемым:

```text
dotnet run
```

Не обязательно уже сейчас контейнеризировать API.

Полный Dockerization будет отдельным инфраструктурным этапом.

---

# 24. 📋 RabbitMQ Management — что научиться смотреть

В UI необходимо самостоятельно найти:

### Exchanges

Какие exchanges созданы?

### Queues

Какие queues существуют?

Например:

```text
notification
audit
```

### Consumers

Какой consumer подключён к queue?

### Messages

Есть ли:

```text
Ready
Unacked
```

сообщения?

### Bindings

Как exchange связан с queue?

---

# 25. 🧪 Практический сценарий №1

Создать:

```text
Project
```

Проверить:

```text
ProjectCreatedEvent
```

Flow:

```text
POST /projects
       ↓
ProjectService
       ↓
PostgreSQL
       ↓
ProjectCreatedEvent
       ↓
RabbitMQ
       ↓
┌───────────────┬───────────────┐
▼               ▼
Notification    Audit
Consumer        Consumer
▼               ▼
SignalR         AuditLog
```

---

# 26. 🧪 Практический сценарий №2

Создать Task.

Проверить:

```text
TaskCreatedEvent
```

После этого:

```text
Redis
```

не должен получать данные напрямую от TaskService, если используется messaging-based invalidation.

---

# 27. 🧪 Практический сценарий №3

Изменить Task:

```text
PUT /tasks/{id}
```

Проверить:

```text
PostgreSQL updated
       ↓
TaskUpdatedEvent
       ↓
RabbitMQ
       ├── Notification
       ├── Audit
       └── Cache invalidation
```

---

# 28. 🧪 Практический сценарий №4

Удалить Task.

Особенно проверить:

```text
audience
```

потому что после удаления получить её из Task уже нельзя.

Flow:

```text
Calculate audience
       ↓
Delete Task
       ↓
SaveChanges
       ↓
TaskDeletedEvent
       ↓
RabbitMQ
```

---

# 29. 🧪 Практический сценарий №5

Удалить Project.

Проверить:

```text
ProjectDeletedEvent
```

и убедиться, что notification consumer отправляет уведомление именно той аудитории, которая была определена **до удаления**.

---

# 30. 🧠 Что НЕ делать в этом спринте

Чтобы не превратить Sprint 6 в огромный distributed system, пока не добавлять:

### ❌ Microservices

Не выделяем:

```text
User Service
Task Service
Notification Service
```

пока всё остаётся монолитом.

---

### ❌ Kafka

Kafka будет следующим инфраструктурным этапом после RabbitMQ.

---

### ❌ gRPC

gRPC понадобится, когда появятся отдельные сервисы.

---

### ❌ Kubernetes

Пока вообще не нужен.

---

### ❌ Outbox как обязательную часть

Но:

> **обязательно понять проблему Dual Write и зачем нужен Outbox.**

Реализацию можно вынести в отдельный hardening/следующий этап.

---

### ❌ CQRS / MediatR

Не возвращаем.

Текущая архитектура:

```text
Controller
    ↓
Service
    ↓
Repository
```

остаётся.

Messaging добавляется как инфраструктурный механизм:

```text
Service
    ↓
IEventPublisher
    ↓
MassTransit
    ↓
RabbitMQ
```

---

# 31. 🏗️ Итоговая архитектура Sprint 6

После завершения должна получиться примерно такая структура:

```text
                         Client
                           │
                    ┌──────┴──────┐
                    │             │
                  REST          SignalR
                    │             ▲
                    ▼             │
                API / Hub         │
                    │             │
                    ▼             │
                Application       │
                    │             │
          ┌─────────┼─────────┐   │
          ▼         ▼         ▼   │
     Repository   Cache    Event   │
          │                 │      │
          ▼                 ▼      │
     PostgreSQL        MassTransit │
                            │      │
                            ▼      │
                        RabbitMQ   │
                       /    |     \
                      /     |      \
                     ▼      ▼       ▼
               Notification Audit  Cache
                 Consumer Consumer Consumer
                     │       │       │
                     ▼       ▼       ▼
                  SignalR  AuditDB  Redis
```

---

# 32. 📁 Предполагаемая структура кода

Ориентировочно:

```text
TaskFlow
│
├── Domain
│
├── Application
│   ├── Interfaces
│   │   └── Messaging
│   │       └── IEventPublisher.cs
│   │
│   └── Events
│       ├── TaskCreatedEvent.cs
│       ├── TaskUpdatedEvent.cs
│       ├── TaskDeletedEvent.cs
│       ├── ProjectCreatedEvent.cs
│       └── ...
│
├── Infrastructure
│   ├── Messaging
│   │   ├── MassTransitEventPublisher.cs
│   │   ├── Consumers
│   │   │   ├── NotificationConsumer.cs
│   │   │   ├── AuditConsumer.cs
│   │   │   └── CacheInvalidationConsumer.cs
│   │   │
│   │   └── Configuration
│   │
│   ├── Persistence
│   │
│   └── Caching
│
├── API
│
└── API.Tests
```

Структуру можно адаптировать под существующую архитектуру проекта.

Не нужно механически создавать папки только ради совпадения с roadmap.

---

# 33. 📚 Теория, которую нужно уметь объяснить

После спринта ты должен своими словами объяснять:

## RabbitMQ

- Что такое RabbitMQ?
- Зачем нужен message broker?
- Producer?
- Consumer?
- Queue?
- Exchange?
- Binding?
- Routing?
- Acknowledgement?
- Redelivery?
- Dead-letter?
- Почему message может прийти повторно?

---

## MassTransit

- Зачем нужен MassTransit?
- Чем он отличается от RabbitMQ.Client?
- Что такое Consumer?
- Что такое Publish?
- Что такое Send?
- Что такое endpoint?
- Как работает retry?
- Как обрабатываются ошибки?

---

## Event-driven architecture

- Что такое event-driven architecture?
- Чем event отличается от command?
- Почему producer не должен знать consumers?
- Почему один event может иметь несколько consumers?
- Что такое loose coupling?
- Что такое eventual consistency?

---

## Distributed systems

Понимать:

```text
at-most-once
at-least-once
exactly-once
```

и почему:

> exactly-once processing в распределённой системе нельзя просто предположить.

---

## Reliability

Уметь объяснить:

- retry;
- idempotency;
- duplicate messages;
- ordering;
- dead-letter;
- consumer failure;
- broker failure.

---

# 34. 🎤 Вопросы для собеседования

После Sprint 6 обязательно уметь отвечать:

### 1.

> Зачем RabbitMQ, если у нас уже есть REST?

Ответ должен затрагивать:

```text
synchronous vs asynchronous
decoupling
background processing
reliability
multiple consumers
```

---

### 2.

> Чем RabbitMQ отличается от Redis?

Нужно понимать:

```text
Redis
→ data store / cache / temporary state

RabbitMQ
→ message broker
```

---

### 3.

> Что произойдёт, если consumer упадёт?

Объяснить:

```text
retry
redelivery
error handling
dead-letter/fault
```

---

### 4.

> Может ли сообщение прийти два раза?

Да.

Поэтому:

```text
consumer should be idempotent
```

---

### 5.

> Что если PostgreSQL успешно сохранил Task, но RabbitMQ недоступен?

Объяснить:

```text
Dual Write Problem
```

и назвать:

```text
Outbox Pattern
```

как решение.

---

### 6.

> Почему TaskService не должен напрямую вызывать SignalR?

Потому что тогда он жёстко связан с transport/notification mechanism.

Лучше:

```text
TaskService
   ↓
Event
   ↓
Notification Consumer
   ↓
SignalR
```

---

### 7.

> Почему один event имеет несколько queues?

Чтобы независимые consumers получили собственную копию сообщения:

```text
notification queue
audit queue
```

---

### 8.

> Чем Publish отличается от Send?

Нужно понимать концептуально:

```text
Publish
→ событие для подписчиков

Send
→ сообщение конкретному endpoint
```

---

# 35. 🧹 Рефакторинг

После того как всё заработало, пройтись по проекту и удалить:

- старые in-process handlers;
- старый `IAppEventPublisher`, если он больше не используется;
- дублирующую notification logic;
- прямые вызовы SignalR из domain/application services;
- лишние зависимости.

Но делать это **только после успешной миграции**.

---

# 36. 📝 README

Обновить README.

Добавить:

```text
### Sprint 6 — RabbitMQ + MassTransit
```

Описать:

- event-driven architecture;
- RabbitMQ;
- MassTransit;
- producers;
- consumers;
- notification consumer;
- audit consumer;
- cache invalidation;
- retry;
- idempotency;
- asynchronous processing.

---

# 37. 📊 Обновлённая схема проекта

После Sprint 6 README должен отражать:

```text
                           Client
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
                 REST API            SignalR
                    │
                    ▼
               Application
                    │
          ┌─────────┼─────────┐
          ▼         ▼         ▼
     PostgreSQL   Redis    Event Publisher
                              │
                              ▼
                         MassTransit
                              │
                              ▼
                          RabbitMQ
                     ┌────────┼────────┐
                     ▼        ▼        ▼
               Notification Audit   Cache
                 Consumer  Consumer Consumer
                     │        │        │
                     ▼        ▼        ▼
                  SignalR   AuditDB   Redis
```

---

# 38. 🏁 Definition of Done

Sprint 6 считается завершённым, если выполнены все пункты ниже.

## RabbitMQ

- [ ] RabbitMQ добавлен в Docker Compose
- [ ] Management UI доступен
- [ ] API подключается к RabbitMQ
- [ ] credentials находятся в configuration/secrets
- [ ] Docker networking понятен

---

## MassTransit

- [ ] MassTransit подключён
- [ ] RabbitMQ transport настроен
- [ ] producer работает
- [ ] consumer работает
- [ ] publish работает
- [ ] queues создаются
- [ ] bindings понятны

---

## Events

- [ ] Event contracts вынесены отдельно
- [ ] Events не являются EF entities
- [ ] Events имеют EventId
- [ ] Events имеют OccurredAt
- [ ] Task events работают
- [ ] Project events работают
- [ ] Comment events работают

---

## Notification

- [ ] Notification consumer получает события
- [ ] SignalR вызывается из consumer
- [ ] существующая аудитория сохраняется
- [ ] `Clients.Users(...)` используется
- [ ] delete-аудитория вычисляется до удаления

---

## Audit

- [ ] Audit consumer работает
- [ ] события записываются
- [ ] EventId сохраняется
- [ ] EventType сохраняется
- [ ] duplicate events не создают неконтролируемые дубли

---

## Cache

- [ ] Cache invalidation переведён на messaging
- [ ] `task:{id}` инвалидируется
- [ ] `project:{id}` инвалидируется
- [ ] `tags:all` продолжает работать согласно существующей логике
- [ ] Redis остаётся cache, а не source of truth

---

## Reliability

- [ ] retry настроен
- [ ] количество retry ограничено
- [ ] ошибка consumer не ломает API request после публикации
- [ ] изучен dead-letter/error flow
- [ ] изучена redelivery
- [ ] реализована или продумана idempotency
- [ ] понимается проблема duplicate messages
- [ ] понимается Dual Write Problem

---

## Tests

- [ ] TaskCreated event проверен
- [ ] ProjectCreated event проверен
- [ ] CommentAdded event проверен
- [ ] Notification consumer проверен
- [ ] Audit consumer проверен
- [ ] Cache invalidation проверен
- [ ] retry проверен
- [ ] duplicate event проверен
- [ ] regression REST API пройден
- [ ] frontend не сломан

---

# 39. 🎯 Главный результат Sprint 6

В конце спринта TaskFlow должен перейти от:

```text
Monolith
+
in-process events
```

к:

```text
Monolith
+
Event-driven communication
+
RabbitMQ
+
MassTransit
+
Independent Consumers
```

При этом приложение всё ещё остаётся **монолитом**.

Это специально.

Ты сначала получаешь:

```text
Event-driven monolith
```

а уже потом на его основе переходишь к:

```text
Microservices
```

---

# 🚀 Следующий этап после Sprint 6

После завершения Sprint 6 следующий крупный этап:

# Sprint 7 — Microservices + gRPC

Там уже можно будет начать реальное выделение bounded contexts:

```text
                    API / Gateway
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
     User Service    Task Service   Project Service
          │              │              │
          └──────────────┼──────────────┘
                         │
                        gRPC
                         │
                    RabbitMQ
```

Но **не начинай Sprint 7, пока Sprint 6 полностью не закрыт**.

Сначала нужно действительно понять RabbitMQ, MassTransit, consumers, retry, idempotency и event-driven architecture. Тогда переход к микросервисам будет не просто «разнести проекты по папкам», а осознанным архитектурным шагом.