# Habit Tracker MVP

Минимальный MVP: **C# ASP.NET Core backend** хранит бизнес-логику и данные, а **Go integration service** отвечает за Telegram, email и доставку уведомлений.

## Что входит

### C# backend

- Пользователи.
- Привычки.
- Выполнение привычек за дату.
- Подсчёт `currentStreak` и `bestStreak`.
- Генерация notification jobs.
- Единый формат ошибок.
- SQLite через EF Core.
- Swagger.
- Unit-тесты для streak, duplicate completion и генерации уведомлений.

### Go service

- Telegram-команды:
  - `/start`
  - `/habits`
  - `/done`
  - `/done 1`
  - `/stats`
  - `/new Drink water`
- HTTP-клиент к C# backend.
- Polling pending notifications из C#.
- Отправка Telegram и email.
- Дополнительные HTTP endpoints:
  - `POST /api/notifications/telegram`
  - `POST /api/notifications/email`

## Архитектура

```text
[Telegram User]
      |
      v
[Go Telegram Bot + Email Service]
      |
      | HTTP JSON
      v
[C# ASP.NET Core Backend]
      |
      v
[SQLite Database]
```

C# — source of truth. Go не считает streak и не хранит основную бизнес-логику.

## Структура

```text
habit-tracker-mvp/
  backend/
    src/
      HabitTracker.Domain/
      HabitTracker.Application/
      HabitTracker.Infrastructure/
      HabitTracker.Api/
    tests/
      HabitTracker.Tests/
  integration-go/
    cmd/bot/
    internal/config/
    internal/email/
    internal/habitapi/
    internal/notifications/
    internal/server/
    internal/telegram/
  docker-compose.yml
```

## Запуск через Docker Compose

```bash
docker compose up --build
```

Backend будет доступен здесь:

```text
http://localhost:5000/swagger
```

Go service будет доступен здесь:

```text
http://localhost:8081/health
```

## Запуск C# backend вручную

```bash
cd backend/src/HabitTracker.Api
dotnet restore
dotnet run
```

Swagger:

```text
http://localhost:5000/swagger
```

## Запуск тестов C#

```bash
cd backend
dotnet test
```

## Запуск Go service вручную

```bash
cd integration-go
go run ./cmd/bot
```

Для реального Telegram-бота нужно задать переменные окружения:

```bash
export TELEGRAM_BOT_TOKEN="your-token"
export BACKEND_URL="http://localhost:5000"
go run ./cmd/bot
```

Для email:

```bash
export SMTP_HOST="smtp.example.com"
export SMTP_PORT="587"
export SMTP_USERNAME="user@example.com"
export SMTP_PASSWORD="password"
export EMAIL_FROM="user@example.com"
```

Если SMTP не настроен, email-сервис просто выводит письмо в консоль. Это удобно для демонстрации.

## Минимальный сценарий проверки через Swagger

1. Создать пользователя:

```http
POST /api/users
```

```json
{
  "name": "Ivan",
  "email": "ivan@mail.com",
  "telegramChatId": "123456789"
}
```

2. Создать привычку:

```http
POST /api/habits
```

```json
{
  "userId": "USER_ID_FROM_STEP_1",
  "title": "Drink water",
  "description": "Drink 2 liters of water",
  "reminderTime": "09:00",
  "notifyInTelegram": true,
  "notifyByEmail": true
}
```

3. Получить привычки пользователя:

```http
GET /api/users/{userId}/habits
```

4. Отметить привычку выполненной:

```http
POST /api/habits/{habitId}/completions
```

```json
{
  "date": "2026-05-18"
}
```

5. Получить статистику:

```http
GET /api/habits/{habitId}/stats
```

6. Получить уведомления, которые пора отправить:

```http
GET /api/notification-jobs/due?before=2026-05-18T09:01:00
```

## Основные C# endpoints

```text
POST /api/users
GET /api/users/by-telegram/{telegramChatId}
POST /api/habits
GET /api/users/{userId}/habits
POST /api/habits/{habitId}/completions
GET /api/habits/{habitId}/stats
GET /api/notification-jobs/due
POST /api/notification-jobs/{jobId}/sent
POST /api/notification-jobs/{jobId}/failed
```

## Основные Go endpoints

```text
GET /health
POST /api/notifications/telegram
POST /api/notifications/email
```
