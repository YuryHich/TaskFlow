# 📊 TaskFlow — Sprint 8 Roadmap

## Apache Kafka + Analytics Service

**Предыдущий этап:** Microservices + gRPC  
**Цель:** добавить Kafka как отдельный event streaming pipeline и построить самостоятельный Analytics Service.

---

# 🎯 Главная идея Sprint 8

До этого:

```text
RabbitMQ
   ↓
Notification
Audit
Cache
```

RabbitMQ используется преимущественно для:

> доставки сообщений конкретным consumers.

Теперь добавляем Kafka для:

> хранения и потоковой обработки потока событий.

---

# 0. 🧠 RabbitMQ vs Kafka

Это одна из главных тем спринта.

## RabbitMQ

```text
Producer
   ↓
Exchange
   ↓
Queue
   ↓
Consumer
```

Основной акцент:

```text
message delivery
```

---

## Kafka

```text
Producer
   ↓
Topic
   ↓
Partition
   ↓
Consumer Group
   ↓
Consumer
```

Основной акцент:

```text
event streaming
+
durable event log
+
replay
```

---

# 0.1. Главное отличие

RabbitMQ:

```text
Message
   ↓
Consumer
```

Kafka:

```text
Event
   ↓
Topic
   ↓
stored
   ↓
Consumer
```

Consumer может читать поток с определённого offset.

---

# 1. 🐘 Kafka в Docker Compose

Добавить Kafka в инфраструктуру.

Цель:

```text
PostgreSQL
Redis
RabbitMQ
Kafka
```

Kafka должна запускаться локально через Compose.

---

# 1.1. Kafka configuration

Разобраться с:

- broker;
- listener;
- advertised listener;
- port;
- topic;
- partition;
- replication factor.

Особенно важно понять:

```text
advertised.listeners
```

потому что неправильная конфигурация часто приводит к:

> клиент подключился к Kafka, но не может получить metadata.

---

# 2. 🧩 Kafka topics

Создать отдельный topic для TaskFlow events.

Например:

```text
taskflow.events
```

---

# 2.1. Альтернативный вариант

Разделить:

```text
taskflow.tasks
taskflow.projects
taskflow.users
```

Но для первого варианта лучше начать с:

```text
taskflow.events
```

и единого event envelope.

---

# 2.2. Partitions

Создать несколько partitions.

Например:

```text
taskflow.events

Partition 0
Partition 1
Partition 2
```

Понять:

> Kafka масштабирует обработку через partitions.

---

# 3. 📦 Event Envelope

Не отправлять произвольный JSON.

Создать общий envelope:

```text
EventId
EventType
OccurredAt
CorrelationId
CausationId
Version
Payload
```

Например:

```json
{
  "eventId": "...",
  "eventType": "task.created",
  "occurredAt": "...",
  "correlationId": "...",
  "version": 1,
  "payload": {}
}
```

---

# 3.1. Зачем EventType

Consumer должен понимать:

```text
task.created
task.updated
task.deleted
```

без необходимости угадывать структуру сообщения.

---

# 3.2. Version

Сразу заложить:

```text
version = 1
```

Чтобы в будущем можно было сделать:

```text
version = 2
```

без мгновенного разрушения consumers.

---

# 4. 📤 Kafka Producer

Создать abstraction:

```text
IEventStreamPublisher
```

Application не должна знать:

```text
KafkaProducer
```

напрямую.

---

# 4.1. Producer flow

Например:

```text
Task Service
   ↓
TaskCreatedEvent
   ↓
Kafka Producer
   ↓
taskflow.events
```

---

# 4.2. Key

Очень важная тема Kafka.

Использовать key, например:

```text
TaskId
```

для task events.

Почему?

Kafka старается сохранять порядок сообщений с одинаковым key внутри одной partition.

Например:

```text
TaskId = 42

TaskUpdated
TaskUpdated
TaskDeleted
```

будут направляться с одинаковым key.

---

# 5. 📥 Analytics Consumer

Создать отдельный:

```text
Analytics Service
```

Он не должен зависеть от REST API.

Архитектура:

```text
Kafka
  ↓
Analytics Service
  ↓
Analytics DB
```

---

# 5.1. Consumer Group

Создать:

```text
analytics-consumer-group
```

Analytics Service работает как отдельная consumer group.

Это важно:

```text
taskflow.events
        │
   ┌────┴───────────┐
   ▼                ▼
analytics-group   future-group
```

Каждая группа самостоятельно читает события.

---

# 6. 🗄️ Analytics Database

Не писать аналитические данные обратно в Task DB.

Создать:

```text
AnalyticsDb
```

Например:

```text
TaskStatistics
ProjectStatistics
UserStatistics
```

---

# 6.1. Почему отдельная БД

Analytics queries могут быть тяжёлыми:

```text
COUNT
GROUP BY
AVG
DATE_TRUNC
```

и не должны тормозить основную Task database.

---

# 7. 📈 Task Statistics

Первый аналитический use case:

```text
Task statistics
```

Хранить:

```text
TotalTasks
CompletedTasks
InProgressTasks
BacklogTasks
```

---

# 7.1. Status transitions

Очень полезно хранить:

```text
TaskStatusChangedEvent
```

и считать:

```text
Backlog → ToDo
ToDo → InProgress
InProgress → Review
Review → Done
```

---

# 7.2. Среднее время выполнения

Можно вычислять:

```text
CreatedAt
CompletedAt
```

и:

```text
CompletionTime
```

Например:

```text
Average task completion time
```

---

# 8. 👤 User Analytics

Создать:

```text
UserStatistics
```

Например:

```text
TasksCreated
TasksCompleted
TasksAssigned
CommentsAdded
```

---

# 8.1. Не использовать live joins

Не делать:

```text
AnalyticsDb
   ↓
JOIN
   ↓
TaskDb
```

А строить статистику на событиях.

Это важная часть event-driven architecture.

---

# 9. 🏢 Project Analytics

Создать:

```text
ProjectStatistics
```

Например:

```text
TaskCount
CompletedTaskCount
ActiveTaskCount
CommentCount
```

Можно дополнить:

```text
CompletionRate
```

---

# 10. 📊 Analytics API

Analytics Service должен иметь собственный REST API.

Например:

```text
GET /api/analytics/tasks
GET /api/analytics/projects
GET /api/analytics/users/{id}
GET /api/analytics/projects/{id}
```

---

# 10.1. Authorization

Analytics не должна автоматически становиться public.

Например:

```text
Admin / Manager
```

получают доступ к общей статистике.

Developer:

```text
свою статистику
```

если это соответствует бизнес-правилам проекта.

---

# 11. 🌐 Gateway → Analytics

React не должен знать прямой адрес Analytics Service.

Схема:

```text
React
  ↓
Gateway
  ↓
Analytics Service
  ↓
Analytics DB
```

---

# 12. 📊 Frontend Analytics Dashboard

Добавить страницу:

```text
Analytics
```

Например:

```text
┌─────────────────────────────────────┐
│ TaskFlow Analytics                  │
├─────────────────────────────────────┤
│ Total Tasks       124               │
│ Completed         78                │
│ In Progress       31                │
│ Backlog           15                │
├─────────────────────────────────────┤
│ Completion Rate   62.9%             │
│ Avg completion    3.4 days          │
└─────────────────────────────────────┘
```

---

# 12.1. Project dashboard

Например:

```text
Project Alpha

Tasks
████████████████ 80

Completed
██████████       52

In Progress
█████            18

Backlog
███              10
```

Можно использовать любую подходящую React chart library.

Главное — аналитика должна приходить из:

```text
Analytics Service
```

а не вычисляться на frontend.

---

# 13. 🔄 Event Processing

Analytics Consumer должен обрабатывать:

```text
TaskCreatedEvent
TaskUpdatedEvent
TaskDeletedEvent
TaskStatusChangedEvent
TaskAssignedEvent

ProjectCreatedEvent
ProjectUpdatedEvent
ProjectDeletedEvent

CommentAddedEvent
CommentDeletedEvent

UserCreatedEvent
UserDeletedEvent
```

Не обязательно все события реализовать сразу.

Начать с:

```text
TaskCreated
TaskUpdated
TaskDeleted
TaskStatusChanged
```

---

# 14. ♻️ Idempotency

Kafka тоже не отменяет проблему повторной обработки.

Consumer должен корректно переживать:

```text
Event A
Event A
```

---

# 14.1. Processed events

Создать:

```text
ProcessedEvent
```

например:

```text
EventId
ProcessedAt
```

с unique constraint:

```text
UNIQUE(EventId)
```

---

# 15. 💾 Offset

Понять:

```text
offset
```

как позицию consumer в partition.

Например:

```text
0
1
2
3
4
5
```

Consumer обработал:

```text
0
1
2
```

и продолжает:

```text
3
```

---

# 15.1. Consumer restart

Остановить Analytics Service.

Опубликовать события.

Запустить его снова.

Проверить, что consumer продолжает обработку с сохранённой позиции согласно выбранной стратегии commit.

---

# 16. 🔁 Replay

Одна из сильных сторон Kafka.

Если аналитическая БД была потеряна:

```text
Analytics DB
   ↓
empty
```

можно потенциально:

```text
Kafka
   ↓
replay events
   ↓
rebuild Analytics DB
```

Это необходимо попробовать на практике.

---

# 17. 🧨 Consumer failure

Сделать искусственную ошибку:

```text
Analytics Consumer
   ↓
throw Exception
```

Посмотреть:

- что происходит;
- когда offset commit;
- повторяется ли сообщение;
- что происходит после restart.

---

# 18. 🧵 Partition ordering

Проверить экспериментом:

```text
Task 42
   ↓
Updated
Updated
Deleted
```

с одним key:

```text
42
```

и убедиться, что события для одного ключа сохраняют порядок внутри partition.

После этого изменить key и понять:

> глобального порядка между всеми partitions нет.

---

# 19. 📈 Analytics aggregation

Не обязательно каждый запрос делать:

```text
SELECT COUNT(*)
```

по огромной таблице events.

Можно поддерживать materialized counters:

```text
ProjectStatistics
```

и обновлять:

```text
TotalTasks++
CompletedTasks++
```

по событиям.

---

# 19.1. Eventual consistency

После:

```text
POST /tasks
```

может быть:

```text
Task DB
   ↓
updated immediately

Analytics DB
   ↓
updated slightly later
```

Это нормально.

Не ожидать:

```text
POST response
   ↓
Analytics DB updated synchronously
```

---

# 20. 🕐 Time-based analytics

Добавить статистику за период:

```text
GET /analytics/tasks?from=...&to=...
```

Например:

```text
Tasks created today
Tasks completed this week
Tasks completed this month
```

---

# 21. 📅 Daily statistics

Можно создать:

```text
DailyTaskStatistics
```

Например:

```text
Date
Created
Completed
Deleted
```

Получится:

```text
Date        Created   Completed
2026-09-10     14         8
2026-09-11     19        12
2026-09-12     11        15
```

---

# 22. 📊 Productivity metrics

Можно добавить:

```text
Completion Rate
Average Completion Time
Tasks per User
Tasks per Project
```

Но не превращать Sprint 8 в полноценный BI-сервис.

Цель:

> показать практическое применение Kafka.

---

# 23. 🔍 Kafka Management

Научиться самостоятельно проверять:

- topics;
- partitions;
- consumer groups;
- offsets;
- lag;
- messages.

---

# 23.1. Consumer lag

Очень важное понятие.

Если Kafka получает:

```text
1000 events/sec
```

а consumer обрабатывает:

```text
500 events/sec
```

то lag растёт.

Нужно понимать:

```text
Producer rate
vs
Consumer processing rate
```

---

# 24. 📈 Monitoring basics

Для Analytics Service смотреть:

```text
Consumer lag
Processing rate
Errors
Retry count
DB latency
```

Пока достаточно логирования и базовых метрик.

Полноценный Prometheus/Grafana можно оставить на infrastructure sprint.

---

# 25. 🧩 RabbitMQ + Kafka

После Sprint 8 в проекте будут одновременно:

```text
RabbitMQ
```

и:

```text
Kafka
```

Это нормально.

Они выполняют разные задачи.

---

# 25.1. RabbitMQ

```text
Task event
   ↓
Notification
Audit
Cache
```

Задача:

> надёжно доставить сообщение конкретным consumers.

---

# 25.2. Kafka

```text
Task event
   ↓
Analytics
Future ML
Future Reporting
Future Data Warehouse
```

Задача:

> хранить и распространять поток событий.

---

# 26. 🧠 Event flow

Целевая схема:

```text
                      Task Service
                           │
                    TaskCreatedEvent
                           │
             ┌─────────────┴─────────────┐
             ▼                           ▼
         RabbitMQ                       Kafka
             │                           │
       ┌─────┴─────┐                     ▼
       ▼           ▼               Analytics Service
 Notification     Audit                  │
       │           │                     ▼
       ▼           ▼                Analytics DB
   SignalR      Audit DB
```

---

# 27. 📨 Дублирование событий

Нужно решить:

> кто публикует события в Kafka?

Вариант на учебном этапе:

```text
Task Service
   ├── RabbitMQ publisher
   └── Kafka publisher
```

Но здесь появляется:

```text
Dual Write
```

между:

```text
RabbitMQ
Kafka
PostgreSQL
```

Не делать вид, что проблемы нет.

---

# 27.1. Обязательно изучить Outbox

После появления Kafka необходимость Outbox становится ещё очевиднее.

В перспективе:

```text
Task DB
   │
   ├── Task
   └── OutboxMessage
          │
          ▼
      Dispatcher
       /       \
      ▼         ▼
 RabbitMQ     Kafka
```

---

# 28. 🧱 Outbox — теория

Разобраться:

> как гарантировать, что DB change и event publication не потеряются независимо друг от друга.

Понять:

```text
transaction
+
outbox table
+
background publisher
```

В Sprint 8 Outbox можно оставить как:

```text
advanced hardening
```

если базовая Kafka pipeline уже работает.

---

# 29. 🧪 Integration tests

Минимум:

### Kafka producer

```text
Event published
```

### Analytics consumer

```text
Event consumed
```

### Idempotency

```text
same EventId twice
→ one logical result
```

### Restart

```text
consumer stop
→ events
→ consumer start
→ processing continues
```

### Replay

```text
Analytics DB reset
→ replay
→ statistics rebuilt
```

---

# 30. 🧪 E2E сценарий

Главный сценарий:

```text
React
  ↓
Gateway
  ↓
Task Service
  ↓
PostgreSQL
  │
  ├───────────────┐
  ▼               ▼
RabbitMQ         Kafka
  │               │
  ▼               ▼
Notification   Analytics
  │               │
  ▼               ▼
SignalR       Analytics DB
  │               │
  ▼               ▼
React          Dashboard
```

---

# 31. 🐳 Docker Compose

В конце:

```text
docker compose up -d
```

должен поднимать:

```text
PostgreSQL
Redis
RabbitMQ
Kafka
User Service
Task Service
Project Service
Notification Service
Audit Service
Analytics Service
Gateway
```

Не обязательно сразу иметь отдельный PostgreSQL container на каждый сервис.

Можно использовать:

```text
PostgreSQL instance
├── UserDb
├── TaskDb
├── ProjectDb
└── AnalyticsDb
```

---

# 32. 📚 Теория для собеседования

После Sprint 8 необходимо уметь объяснить:

### Kafka

- broker;
- topic;
- partition;
- offset;
- consumer;
- consumer group;
- producer;
- key;
- ordering;
- replication;
- retention;
- consumer lag.

### Kafka vs RabbitMQ

- queue vs topic;
- delivery vs event stream;
- replay;
- consumer groups;
- partitions;
- ordering.

### Event-driven architecture

- event;
- eventual consistency;
- idempotency;
- replay;
- event contract;
- schema evolution.

### Distributed systems

- duplicate events;
- retries;
- failures;
- ordering;
- dual write;
- outbox.

---

# 33. 🎤 Вопросы для собеседования

Ты должен уметь ответить:

### Почему Kafka, если уже есть RabbitMQ?

### Что такое partition?

### Что такое consumer group?

### Что такое offset?

### Что произойдёт, если consumer упадёт?

### Можно ли прочитать сообщение повторно?

### Как Kafka обеспечивает порядок?

### Что такое consumer lag?

### Зачем нужен key?

### Почему Analytics Service имеет отдельную БД?

### Почему analytics eventual consistent?

### Как восстановить Analytics DB?

### Что такое replay?

### Как бороться с duplicate events?

### Что такое Outbox Pattern?

---

# 34. 📝 README

Добавить:

```text
### Sprint 8 — Kafka + Analytics
```

Описать:

- Kafka;
- topics;
- partitions;
- consumer groups;
- Analytics Service;
- Analytics DB;
- event streaming;
- replay;
- idempotency;
- eventual consistency.

---

# 35. 🏁 Definition of Done

## Kafka

- [ ] Kafka работает в Docker Compose
- [ ] topic создан
- [ ] partitions настроены
- [ ] producer работает
- [ ] consumer работает
- [ ] consumer group создана
- [ ] offsets понятны
- [ ] lag понятен
- [ ] key используется
- [ ] ordering проверен

## Analytics Service

- [ ] отдельный сервис
- [ ] отдельная DB
- [ ] Kafka consumer
- [ ] Task statistics
- [ ] Project statistics
- [ ] User statistics
- [ ] REST API
- [ ] authorization
- [ ] eventual consistency

## Reliability

- [ ] idempotency
- [ ] duplicate events
- [ ] consumer restart
- [ ] error handling
- [ ] replay
- [ ] изучен Outbox Pattern

## Frontend

- [ ] Analytics page
- [ ] summary cards
- [ ] project statistics
- [ ] task statistics
- [ ] time-based statistics

## Infrastructure

- [ ] Kafka в Compose
- [ ] Analytics Service запускается
- [ ] Kafka networking работает
- [ ] Gateway маршрутизирует analytics requests

---

# 🎯 Итог Sprint 8

После этого спринта TaskFlow будет иметь уже два разных messaging-подхода:

```text
                 ┌── RabbitMQ
                 │
Task Service ────┤
                 │
                 └── Kafka
```

RabbitMQ:

```text
Operational messaging
Notification
Audit
Cache
```

Kafka:

```text
Event streaming
Analytics
Reporting
Future data processing
```

А итоговая архитектура:

```text
                           Client
                              │
                              ▼
                         API Gateway
                              │
        ┌─────────────┬───────┼──────────────┐
        ▼             ▼       ▼              ▼
   User Service   Task Service Project     Analytics
                              Service       Service
        │             │       │              │
        │             └──gRPC─┘              │
        │                                    │
        └──────────── Events ────────────────┘
                       │
              ┌────────┴────────┐
              ▼                 ▼
          RabbitMQ             Kafka
          /    |    \            │
         ▼     ▼     ▼           ▼
 Notification Audit Cache    Analytics
   Service    Service       Service
      │
      ▼
   SignalR

      Redis                 PostgreSQL
```

---

# 🚀 Следующий этап

После Sprint 8 логично сделать отдельный:

# Infrastructure / Production Hardening Sprint

Туда вынести:

- Dockerfile каждого сервиса;
- полноценный Docker Compose;
- environment variables;
- secrets;
- health checks;
- readiness/liveness;
- structured logging;
- OpenTelemetry;
- Prometheus;
- Grafana;
- distributed tracing;
- correlation IDs;
- centralized configuration;
- RabbitMQ/Kafka health;
- graceful shutdown;
- retry policies;
- Outbox Pattern;
- CI/CD;
- GitHub Actions.

Именно после этого TaskFlow будет выглядеть уже не просто как учебный CRUD-проект, а как достаточно полноценный **distributed backend project**.