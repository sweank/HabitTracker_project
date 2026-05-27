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

- Telegram-команды: `/start`, `/habits`, `/done`, `/done 1`, `/stats`, `/new Drink water`.
- HTTP-клиент к C# backend.
- Polling pending notifications из C#.
- Отправка Telegram и email.
- Дополнительные HTTP endpoints:
  - `GET /health`
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
    Dockerfile
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
  .env.example
```


## Важное исправление для Docker на Windows

В `backend/.dockerignore` обязательно должны быть исключены `bin/` и `obj/`. Иначе Docker может скопировать внутрь Linux-контейнера локальные .NET артефакты, созданные на Windows, и `dotnet publish` упадёт с ошибкой вида:

```text
Unable to find fallback package folder 'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'
```

Если ошибка уже появлялась, можно также удалить локальные артефакты перед повторной сборкой:

```powershell
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
docker compose build --no-cache
docker compose up
```

## Настройка переменных окружения

Скопируйте пример:

```powershell
copy .env.example .env
```

Для Linux/macOS:

```bash
cp .env.example .env
```

В `.env` укажите `TELEGRAM_BOT_TOKEN`, если нужен настоящий Telegram polling. Без токена Go-сервис всё равно запустит HTTP endpoints и worker, но Telegram polling будет отключён.

## Запуск через Docker Compose

```bash
docker compose up --build
```

После запуска:

```text
Backend Swagger: http://localhost:5000/swagger
Go health:       http://localhost:8081/health
```

## Запуск C# backend вручную

```powershell
cd backend
dotnet restore src/HabitTracker.Api/HabitTracker.Api.csproj
dotnet run --project src/HabitTracker.Api/HabitTracker.Api.csproj --urls "http://localhost:5000"
```

Swagger:

```text
http://localhost:5000/swagger
```

## Запуск тестов C#

```powershell
cd backend
dotnet test tests/HabitTracker.Tests/HabitTracker.Tests.csproj
```

## Запуск Go service вручную

PowerShell on Windows:

```powershell
cd integration-go
$env:BACKEND_URL="http://localhost:5000"
$env:GO_HTTP_ADDR=":8081"
$env:TELEGRAM_BOT_TOKEN="your-token"
go run ./cmd/bot
```

Bash/macOS/Linux:

```bash
cd integration-go
export BACKEND_URL="http://localhost:5000"
export GO_HTTP_ADDR=":8081"
export TELEGRAM_BOT_TOKEN="your-token"
go run ./cmd/bot
```

Если токен не задан, Telegram polling отключается, но `GET /health`, отправка email/telegram через HTTP endpoints и notification worker остаются доступны.

## Email настройки

PowerShell:

```powershell
$env:SMTP_HOST="smtp.example.com"
$env:SMTP_PORT="587"
$env:SMTP_USERNAME="user@example.com"
$env:SMTP_PASSWORD="password"
$env:EMAIL_FROM="user@example.com"
```

Bash/macOS/Linux:

```bash
export SMTP_HOST="smtp.example.com"
export SMTP_PORT="587"
export SMTP_USERNAME="user@example.com"
export SMTP_PASSWORD="password"
export EMAIL_FROM="user@example.com"
```

Если SMTP не настроен, email-сервис выводит письмо в консоль. Это удобно для демонстрации.

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
GET /api/notification-jobs/due?before=2026-05-18T09:01:00Z
```

## Основные C# endpoints

```text
GET  /health
POST /api/users
GET  /api/users/by-telegram/{telegramChatId}
POST /api/habits
GET  /api/users/{userId}/habits
POST /api/habits/{habitId}/completions
GET  /api/habits/{habitId}/stats
GET  /api/notification-jobs/due
POST /api/notification-jobs/{jobId}/sent
POST /api/notification-jobs/{jobId}/failed
```

## Основные Go endpoints

```text
GET  /health
POST /api/notifications/telegram
POST /api/notifications/email
```
