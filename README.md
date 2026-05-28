# Habit Tracker

Habit Tracker - учебное API-приложение для отслеживания привычек. Пользователь создает привычки, задает цель выполнения, отмечает выполнение по дням, смотрит статистику и получает напоминания через отдельную Go-интеграцию с Telegram.

## Что реализовано

- REST API на ASP.NET Core 8.
- Swagger UI для проверки всех endpoint'ов.
- Пользователи с привязкой к Telegram ID.
- CRUD для привычек.
- Цель выполнения: daily / weekly и `targetCountPerPeriod`.
- Отметка выполнения привычки по дате.
- Удаление отметки за конкретный день.
- Статистика за период: количество отметок, процент выполнения, текущая серия, максимальная серия.
- Endpoint для напоминаний: привычки с заданным временем, которые еще не выполнены за день.
- Go-интеграция с Telegram Bot API.
- Docker Compose для запуска API и Go-интеграции.
- Демоданные, чтобы проект сразу открывался и проверялся.
- Базовый xUnit-тест для расчета статистики.

## Структура проекта

```text
backend/HabitTracker.Api/       ASP.NET Core API
backend/HabitTracker.Tests/     базовые тесты
integration-go/                 Go-интеграция с Telegram
HabitTracker.http               готовые HTTP-запросы для IDE
.env.example                    пример переменных окружения
docker-compose.yml              запуск проекта в Docker
```

## Быстрый запуск через Docker

1. Скопировать файл переменных окружения:

```bash
cp .env.example .env
```

2. Вставить токен Telegram-бота в `.env`:

```env
TELEGRAM_BOT_TOKEN=ваш_токен
```

Если токен не указать, `integration-go` запустится в режиме проверки backend и не будет подключаться к Telegram.

3. Запустить проект:

```bash
docker compose up --build
```

4. Открыть Swagger:

```text
http://localhost:8080/swagger
```

5. Проверить health endpoint:

```text
http://localhost:8080/health
```

## Быстрый запуск без Telegram

```bash
docker compose up --build habittracker-api
```

Swagger будет доступен по адресу:

```text
http://localhost:8080/swagger
```

## Демопользователь

В проекте есть демопользователь:

```text
userId: 11111111-1111-1111-1111-111111111111
telegramUserId: 100001
username: demo
```

Демопривычки:

```text
Пить воду: 22222222-2222-2222-2222-222222222222
Читать 20 минут: 33333333-3333-3333-3333-333333333333
```

## Основные endpoint'ы

```text
GET    /health
GET    /api/users
POST   /api/users
GET    /api/users/{id}
GET    /api/users/by-telegram/{telegramUserId}
GET    /api/users/{userId}/habits
POST   /api/users/{userId}/habits
GET    /api/habits/{habitId}
PUT    /api/habits/{habitId}
PATCH  /api/habits/{habitId}/archive?isArchived=true
DELETE /api/habits/{habitId}
POST   /api/habits/{habitId}/completions
GET    /api/habits/{habitId}/completions
DELETE /api/habits/{habitId}/completions/{date}
GET    /api/users/{userId}/stats
GET    /api/reminders/due?time=09:00
POST   /api/demo/reset
```

## Команды Telegram-бота

```text
/start - регистрация пользователя и справка
/help - список команд
/habits - список привычек
/add <название> - создать привычку
/done <habitId> - отметить выполнение привычки
/stats - статистика за 30 дней
```

## Локальный запуск backend без Docker

Нужен .NET SDK 8.

```bash
cd backend/HabitTracker.Api
dotnet restore
dotnet run
```

API будет доступно по адресу, который покажет `dotnet run`. Для Swagger обычно используется:

```text
http://localhost:5000/swagger
```

или

```text
http://localhost:8080/swagger
```

## Локальный запуск Go-интеграции

Нужен Go 1.23.

```bash
cd integration-go
set BACKEND_URL=http://localhost:8080
set TELEGRAM_BOT_TOKEN=ваш_токен
go run .
```

Для PowerShell:

```powershell
$env:BACKEND_URL="http://localhost:8080"
$env:TELEGRAM_BOT_TOKEN="ваш_токен"
go run .
```

## Тесты

```bash
dotnet test backend/HabitTracker.Tests
```

## Что не коммитить

- `.env`
- реальные токены Telegram
- локальные файлы данных из `App_Data`
- папки `bin/`, `obj/`

## Как сбросить демоданные

```http
POST http://localhost:8080/api/demo/reset
```

Этот endpoint нужен только для учебного демо и проверки проекта.
