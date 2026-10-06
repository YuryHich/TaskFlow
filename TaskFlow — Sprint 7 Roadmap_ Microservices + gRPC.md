# ⚡ TaskFlow — Sprint 7 Roadmap

## Microservices + gRPC

**Предыдущий этап:** RabbitMQ + MassTransit 8.3.6  
**Текущая архитектура:** event-driven monolith  
**Цель спринта:** начать реальное разделение TaskFlow на bounded contexts и отдельные сервисы, используя RabbitMQ для событий и gRPC для синхронного межсервисного взаимодействия.

---

# 🎯 Главная цель Sprint 7

Сейчас TaskFlow выглядит примерно так:

```text
                         Client
                            │
                  ┌─────────┴─────────┐
                  ▼                   ▼
                REST               SignalR
                  │
                  ▼
              TaskFlow API
                  │
       ┌──────────┼──────────┐
       ▼          ▼          ▼
   PostgreSQL   Redis    Event Publisher
                            │
                            ▼
                        RabbitMQ
                       /         \
                      ▼           ▼
               Notification      Audit
```

После Sprint 7 необходимо получить:

```text
                         Client
                            │
                            ▼
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
                     ┌──────┴──────┐
                     ▼             ▼
                 RabbitMQ        Redis
                     │
             ┌───────┴────────┐
             ▼                ▼
       Notification        Audit
          Service           Service
```

**Но не пытаться сразу построить всю эту схему.**

Sprint 7 — это контролируемая миграция:

```text
Monolith
   ↓
Extract bounded context
   ↓
Separate service
   ↓
gRPC
   ↓
RabbitMQ events
   ↓
API / Gateway
```

---

# 0. 🧠 Теория перед началом

Перед кодом необходимо разобраться, что именно мы называем микросервисом.

## 0.1. Monolith

Сейчас:

```text
TaskFlow.API
├── Users
├── Projects
├── Tasks
├── Comments
├── Tags
├── Auth
├── Notifications
└── Audit
```

Один процесс.

Один deployment.

Один основной database.

---

# 0.2. Microservice

Микросервис — не просто:

> отдельный проект `.csproj`.

Важнее:

- самостоятельная ответственность;
- отдельный deployment;
- независимое масштабирование;
- собственная граница данных;
- независимый lifecycle;
- минимальная связанность.

---

# 0.3. Bounded Context

Не делить систему исключительно по CRUD-контроллерам.

Например:

```text
Users
Tasks
Projects
Notifications
Audit
```

могут быть разными bounded contexts.

---

# 0.4. Главный вопрос

Перед выделением сервиса спросить:

> Какую бизнес-ответственность этот сервис полностью владеет?

---

# 1. 🧩 Выбор первого сервиса

Не выделять сразу всё.

Первым рекомендуется выделить:

# Notification Service

Почему:

```text
Notification
```

уже логически отделён от основной CRUD-логики.

Сейчас:

```text
TaskFlow API
   ↓
RabbitMQ
   ↓
NotificationConsumer
   ↓
SignalR
```

Можно превратить в:

```text
TaskFlow API
   ↓
RabbitMQ
   ↓
Notification Service
   ↓
SignalR
```

---

# 1.1. Первый этап

Создать:

```text
Services/
└── NotificationService/
```

Примерно:

```text
NotificationService
├── NotificationService.Api
├── NotificationService.Application
├── NotificationService.Infrastructure
└── NotificationService.sln
```

Не обязательно сразу делать все четыре проекта, если текущая архитектура позволяет проще.

Главное — реальная отдельная application boundary.

---

# 1.2. Notification Service должен владеть

```text
SignalR
NotificationConsumer
notification-specific logic
```

TaskFlow API больше не должен напрямую реализовывать notification delivery.

---

# 2. 🐇 RabbitMQ между сервисами

Получается:

```text
TaskFlow API
     │
     │ publish TaskCreatedEvent
     ▼
RabbitMQ
     │
     ▼
Notification Service
     │
     ▼
SignalR
```

Это первый настоящий межсервисный communication flow.

---

# 2.1. Почему здесь RabbitMQ

Потому что notification не обязательно выполнять синхронно.

API может:

```text
save task
publish event
return HTTP response
```

а notification service:

```text
receive event
send SignalR notification
```

в отдельном процессе.

---

# 3. 📦 Event Contracts

Очень важный архитектурный момент.

Общие контракты должны быть отделены от domain entities.

Например:

```text
TaskCreatedEvent
TaskUpdatedEvent
TaskDeletedEvent
ProjectCreatedEvent
...
```

Не передавать:

```text
WorkTask
Project
User
```

как integration contracts.

---

# 3.1. Contract package

Можно создать:

```text
TaskFlow.Contracts
```

где находятся только integration contracts.

Например:

```text
TaskFlow.Contracts
├── Tasks
│   ├── TaskCreatedEvent.cs
│   ├── TaskUpdatedEvent.cs
│   └── TaskDeletedEvent.cs
│
├── Projects
│   ├── ProjectCreatedEvent.cs
│   └── ...
│
└── Comments
    └── ...
```

---

# 3.2. Что НЕ должно находиться в Contracts

Не помещать:

```text
DbContext
Entity
Repository
Service
Controller
business logic
```

Contracts должны быть максимально простыми.

---

# 4. 📡 Notification Service

Перенести:

```text
NotificationConsumer
```

из монолита в отдельный процесс.

Получится:

```text
RabbitMQ
    ↓
NotificationService
    ↓
SignalR Hub
```

---

# 4.1. SignalR Hub

Переместить:

```text
/hubs/notifications
```

в Notification Service.

Но сохранить клиентский контракт:

```text
Notify
```

и payload:

```text
{
    eventName,
    projectId,
    taskId,
    commentId
}
```

Чтобы React SPA не пришлось переписывать целиком.

---

# 4.2. JWT

Notification Service должен понимать пользователя.

SignalR:

```text
[Authorize]
```

должен продолжать работать.

Нужно разобраться:

```text
JWT
↓
Claims
↓
UserIdentifier
↓
Clients.Users(...)
```

---

# 5. 🔐 JWT между сервисами

Теперь появляется важный вопрос:

> Кто проверяет JWT?

В первой версии:

```text
API
```

и:

```text
Notification Service
```

могут иметь возможность самостоятельно валидировать JWT.

Для этого необходимо понимать:

```text
issuer
audience
signing key
expiration
```

---

# 5.1. Общая signing key

На учебном этапе допустимо:

```text
API
   ↓
HS256
   ↓
Notification Service
```

используя одну конфигурацию signing key.

Но необходимо понимать проблему:

> при symmetric JWT секрет должен знать каждый сервис, который валидирует токен.

В дальнейшем можно перейти к asymmetric signing:

```text
RS256
```

где сервисы получают public key.

---

# 6. 🌐 API Gateway

Следующий шаг — не заставлять frontend знать адрес каждого сервиса.

Сейчас:

```text
React
  ↓
API
```

После выделения сервисов:

```text
React
  ↓
Gateway
  ├── User Service
  ├── Task Service
  └── Project Service
```

---

# 6.1. Gateway

Создать отдельный:

```text
TaskFlow.Gateway
```

или использовать существующий API как внешний facade на первом этапе.

Главная идея:

```text
Client
   ↓
Gateway
   ↓
Services
```

---

# 6.2. Что gateway делает

Gateway отвечает за:

- внешний HTTP API;
- routing;
- authentication forwarding;
- единый entry point;
- aggregation при необходимости.

---

# 6.3. Что gateway НЕ делает

Не переносить в gateway:

```text
Task business logic
User business logic
Project business logic
```

Gateway не должен становиться новым монолитом.

---

# 7. 👤 User Service

После Notification выделить:

# User Service

Ответственность:

```text
User
Authentication
Authorization-related user data
Refresh tokens
User directory
```

Но делать это поэтапно.

---

# 7.1. Database ownership

Очень важный шаг.

Сейчас:

```text
PostgreSQL
   ↓
Users
Projects
Tasks
Comments
Tags
AuditLogs
```

После микросервисизации:

```text
User Service
   ↓
User DB

Task Service
   ↓
Task DB

Project Service
   ↓
Project DB
```

---

# 7.2. Не делить database table между сервисами

Не делать:

```text
Task Service
    ↓
UserServiceDb.Users
```

через прямой SQL.

Если Task Service нужен User:

```text
Task Service
    ↓
gRPC
    ↓
User Service
```

или:

```text
event
```

---

# 8. 🧱 Task Service

После User Service выделить:

```text
Task Service
```

Он владеет:

```text
WorkTask
Comment
Tag
```

или, если хочется более строгого bounded context:

```text
Task Service
Comment/Tag
```

можно оставить внутри Task Context.

Не нужно делать микросервис на каждую сущность.

---

# 8.1. Task Service API

Например внутренний HTTP API:

```text
GET /tasks/{id}
POST /tasks
PUT /tasks/{id}
DELETE /tasks/{id}
```

Но внешний клиент не обязан обращаться к нему напрямую.

---

# 9. 🏢 Project Service

Следующим:

```text
Project Service
```

Ответственность:

```text
Project
Project ownership
Project metadata
```

---

# 9.1. Task ↔ Project

Здесь появится distributed relationship.

Раньше:

```text
Task.ProjectId
```

и PostgreSQL FK.

Теперь:

```text
Task Service
      │
      │ ProjectId
      ▼
Project Service
```

Task Service не должен иметь foreign key на таблицу Project Service.

---

# 10. 🚀 gRPC

Теперь появляется необходимость в синхронном межсервисном communication.

RabbitMQ:

```text
event happened
```

gRPC:

```text
I need information now
```

---

# 10.1. REST vs gRPC

REST:

```text
HTTP
JSON
public API
browser-friendly
```

gRPC:

```text
HTTP/2
Protocol Buffers
strong contracts
service-to-service
```

---

# 10.2. Когда использовать gRPC

Например:

```text
Task Service
    ↓
"Give me user information"
    ↓
User Service
```

Это request/response.

Здесь event не подходит.

---

# 10.3. Когда использовать RabbitMQ

Например:

```text
TaskCreated
```

Task Service сообщает:

> Task создан.

Notification Service:

> мне это интересно.

Audit Service:

> мне это интересно.

Analytics Service:

> мне это интересно.

---

# 11. 📜 Proto contracts

Создать:

```text
.proto
```

например:

```text
UserService.proto
```

Сервис:

```text
service UserService {
    rpc GetUser(GetUserRequest) returns (UserResponse);
}
```

---

# 11.1. Protobuf

Изучить:

- message;
- service;
- rpc;
- field numbers;
- request;
- response;
- enum;
- repeated.

---

# 11.2. Почему field numbers важны

Например:

```proto
message UserResponse {
    string id = 1;
    string username = 2;
    string email = 3;
}
```

Номера являются частью wire contract.

Нельзя бездумно менять:

```text
id = 1
```

на другое значение.

---

# 12. 🔄 Первый gRPC вызов

Сценарий:

```text
Task Service
     │
     │ gRPC
     ▼
User Service
     │
     ▼
UserResponse
```

Например Task Service проверяет существование assignee.

---

# 12.1. До

```text
TaskService
   ↓
DbContext.Users
```

После:

```text
TaskService
   ↓
IUserClient
   ↓
gRPC
   ↓
UserService
   ↓
User DB
```

---

# 13. 🧩 Application abstraction

Как и с MassTransit:

Application не должен быть жёстко привязан к generated gRPC client.

Лучше:

```text
Application
   ↓
IUserDirectoryClient
   ↓
Infrastructure
   ↓
gRPC
```

---

# 14. ⏱️ gRPC timeout

Каждый межсервисный вызов может зависнуть.

Нужно использовать:

```text
deadline
timeout
CancellationToken
```

Не делать:

```text
await grpcCall;
```

без понимания cancellation.

---

# 15. 💥 gRPC failure

Что если:

```text
User Service
   ↓
DOWN
```

а:

```text
Task Service
```

делает gRPC call?

Необходимо определить:

- timeout;
- retry;
- fallback;
- HTTP mapping;
- error handling.

---

# 15.1. Не делать бесконечные retries

Например:

```text
Task Service
 ↓
User Service
 ↓
timeout
 ↓
retry
 ↓
timeout
```

Количество попыток должно быть ограничено.

---

# 16. 🔄 RabbitMQ + gRPC вместе

Теперь нужно чётко понимать:

```text
RabbitMQ
=
asynchronous communication
```

а:

```text
gRPC
=
synchronous communication
```

Они не конкурируют.

Они решают разные задачи.

---

# 17. 🧠 Distributed authorization

Очень важный этап.

Раньше:

```text
TaskService
   ↓
User table
```

Теперь сервисы не имеют общей таблицы.

Необходимо разделить:

### Authentication

```text
Who are you?
```

### Authorization

```text
Can you perform this action?
```

---

# 17.1. Где проверять authorization

Внешний request:

```text
Gateway
   ↓
JWT
```

Но бизнес authorization должна оставаться в сервисе, который владеет ресурсом.

Например:

```text
PUT /tasks/15
```

Task Service должен сам решить:

> может ли этот пользователь менять Task 15?

Gateway не должен знать всю Task business logic.

---

# 18. 🗄️ Database per service

Это один из ключевых пунктов.

Целевое состояние:

```text
User Service
    ↓
UserDb

Task Service
    ↓
TaskDb

Project Service
    ↓
ProjectDb

Notification Service
    ↓
NotificationDb / no DB

Audit Service
    ↓
AuditDb
```

---

# 18.1. Миграция

Не пытаться мигрировать всю БД одним коммитом.

Делать:

```text
User tables
   ↓
User DB

Task tables
   ↓
Task DB
```

поэтапно.

---

# 19. 🔄 Data consistency

Теперь нельзя делать:

```text
BEGIN TRANSACTION
   User update
   Task update
   Project update
COMMIT
```

если это разные databases.

Вместо этого:

```text
Service A
   ↓
event
   ↓
Service B
```

и eventual consistency.

---

# 20. 📨 Event-driven consistency

Например:

```text
User deleted
```

User Service публикует:

```text
UserDeletedEvent
```

Task Service получает событие:

```text
UserDeletedEvent
```

и выполняет необходимую локальную обработку.

---

# 21. 🧪 Интеграционные тесты

После разделения добавить тесты:

### User Service

- register;
- login;
- user lookup.

### Task Service

- CRUD;
- authorization;
- assignee validation.

### Project Service

- owner;
- project access.

### gRPC

- valid request;
- not found;
- timeout;
- unavailable service.

### RabbitMQ

- event published;
- consumer receives event.

---

# 22. 🐳 Docker

В конце спринта постепенно начать запуск:

```text
docker compose
│
├── postgres-user
├── postgres-task
├── postgres-project
├── redis
├── rabbitmq
├── gateway
├── user-service
├── task-service
├── project-service
└── notification-service
```

Но можно временно оставить некоторые БД общей PostgreSQL instance с разными databases.

Главное — **логическое ownership**, а не количество контейнеров PostgreSQL.

---

# 23. 📊 Observability basics

Микросервисы сильно сложнее отлаживать.

Минимум добавить:

```text
ServiceName
TraceId
CorrelationId
```

в logs.

Например:

```text
Gateway
TraceId=abc123
   ↓
TaskService
TraceId=abc123
   ↓
UserService
TraceId=abc123
```

---

# 24. 🧹 Удаление старого монолита

После успешной миграции удалить из старого API:

- NotificationConsumer;
- прямую notification logic;
- User logic, если полностью перенесена;
- Task logic, если полностью перенесена;
- старые database dependencies.

Но только после того, как новый путь работает.

---

# 25. 🧪 Финальный E2E сценарий

Главный сценарий Sprint 7:

```text
React
  ↓
Gateway
  ↓
Task Service
  │
  ├── gRPC → User Service
  │
  └── PostgreSQL
          │
          ▼
      TaskCreatedEvent
          │
          ▼
       RabbitMQ
          │
          ▼
 Notification Service
          │
          ▼
       SignalR
          │
          ▼
        React
```

---

# 26. 🎤 Что уметь объяснить на собеседовании

После Sprint 7 ты должен уметь ответить:

### Что такое микросервис?

### Что такое bounded context?

### Почему нельзя просто сделать отдельный проект?

### Почему database per service?

### Когда RabbitMQ, а когда gRPC?

### Что такое eventual consistency?

### Что делать, если gRPC service недоступен?

### Как передавать JWT между сервисами?

### Почему Gateway не должен содержать бизнес-логику?

### Почему нельзя делать прямой доступ Task Service к User DB?

### Что такое distributed transaction?

### Что такое service discovery?

### Что такое correlation ID?

---

# 27. 🏁 Definition of Done

## Архитектура

- [ ] Notification Service выделен
- [ ] User Service выделен
- [ ] Task Service выделен
- [ ] Project Service выделен
- [ ] bounded contexts определены
- [ ] ownership данных определён

## RabbitMQ

- [ ] межсервисные события работают
- [ ] consumers работают отдельно от API
- [ ] Notification Service получает события

## gRPC

- [ ] `.proto` contracts
- [ ] generated clients
- [ ] server implementation
- [ ] client abstraction
- [ ] timeout
- [ ] cancellation
- [ ] error handling

## Data

- [ ] сервисы не читают чужие таблицы напрямую
- [ ] database ownership определён
- [ ] нет cross-service foreign keys

## Gateway

- [ ] единая внешняя точка входа
- [ ] routing
- [ ] JWT forwarding
- [ ] бизнес-логика не находится в gateway

## Tests

- [ ] unit tests
- [ ] integration tests
- [ ] gRPC tests
- [ ] messaging tests
- [ ] E2E сценарий

## Infrastructure

- [ ] Docker Compose запускает сервисы
- [ ] RabbitMQ работает
- [ ] PostgreSQL работает
- [ ] Redis работает
- [ ] сервисы видят друг друга по Docker network

---

# 🎯 Итог Sprint 7

Ты переходишь от:

```text
Event-driven Monolith
```

к:

```text
Distributed Event-driven System
```

с:

```text
REST
+
Gateway
+
Microservices
+
gRPC
+
RabbitMQ
+
Redis
+
SignalR
```

Следующий этап:

# Sprint 8 — Kafka + Analytics