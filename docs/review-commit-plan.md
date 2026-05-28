# План доработок и коммитов для ревью

Цель доработок - закрыть пять групп критериев: слои, абстракции, тесты, явное управление зависимостями и обработка ошибок.

## Участник 1 - Go integration

Зона ответственности: `integration-go`.

### Коммит 1: Go config and API client abstractions
Файлы:
- `integration-go/cmd/bot/main.go`
- `integration-go/internal/config/config.go`
- `integration-go/internal/config/config_test.go`
- `integration-go/internal/habitapi/client.go`
- `integration-go/internal/habitapi/models.go`
- `integration-go/internal/habitapi/client_test.go`

Что показать на ревью:
- конфиг валидируется до запуска;
- HTTP-клиент backend вынесен за интерфейс `HTTPDoer`;
- client поддерживает dependency injection через `WithHTTPClient`;
- backend ошибки декодируются в `APIError`.

### Коммит 2: Go HTTP endpoints error handling
Файлы:
- `integration-go/internal/server/server.go`
- `integration-go/internal/server/server_test.go`

Что показать на ревью:
- endpoint-ы проверяют обязательные поля;
- неизвестные JSON-поля отклоняются;
- ответы об ошибках имеют единый формат `errorCode` и `message`;
- есть тесты на health, validation и ошибку отправителя.

### Коммит 3: Telegram handler tests
Файлы:
- `integration-go/internal/telegram/handler.go`
- `integration-go/internal/telegram/handler_test.go`
- `integration-go/internal/notifications/worker.go`
- `integration-go/internal/notifications/worker_test.go`

Что показать на ревью:
- Telegram handler зависит от интерфейса `HabitAPI`, а не от конкретного клиента;
- notification worker зависит от интерфейсов отправки;
- покрыты сценарии `/start`, `/new`, разбор команд и выбор канала уведомления.

Команды проверки:
```bash
cd integration-go
go test ./...
go run ./cmd/bot
```

## Участник 2 - Backend Application/API

Зона ответственности: API и application layer.

### Коммит 1: Validation abstraction
Файлы:
- `backend/src/HabitTracker.Application/Common/IInputValidator.cs`
- `backend/src/HabitTracker.Application/Validation/CreateUserRequestValidator.cs`
- `backend/src/HabitTracker.Application/Validation/CreateHabitRequestValidator.cs`
- `backend/src/HabitTracker.Application/DependencyInjection.cs`
- `backend/src/HabitTracker.Application/Services/UserService.cs`
- `backend/src/HabitTracker.Application/Services/HabitService.cs`
- `backend/tests/HabitTracker.Tests/ValidationTests.cs`

Что показать на ревью:
- валидация вынесена из сервисов в отдельные классы;
- сервисы получают валидаторы через DI;
- ошибки валидации возвращаются через `AppException`;
- добавлены unit-тесты на валидаторы.

### Коммит 2: API error handling
Файлы:
- `backend/src/HabitTracker.Api/Middleware/ApiExceptionMiddleware.cs`
- `backend/src/HabitTracker.Application/DTOs/ErrorResponse.cs`

Что показать на ревью:
- единый формат ошибок API;
- middleware обрабатывает application errors, плохие HTTP-запросы и unexpected errors;
- в ответ добавлен `traceId`, чтобы проще искать ошибку в логах.

Команды проверки:
```bash
cd backend
dotnet test tests/HabitTracker.Tests/HabitTracker.Tests.csproj
dotnet run --project src/HabitTracker.Api/HabitTracker.Api.csproj --urls "http://localhost:5000"
```

## Участник 3 - Backend Domain/Infrastructure/tests

Зона ответственности: domain, infrastructure и тестовое покрытие backend.

### Коммит 1: Notification message abstraction
Файлы:
- `backend/src/HabitTracker.Application/Interfaces/Services.cs`
- `backend/src/HabitTracker.Application/Services/NotificationMessageFactory.cs`
- `backend/src/HabitTracker.Application/Services/NotificationJobService.cs`
- `backend/src/HabitTracker.Application/DependencyInjection.cs`
- `backend/tests/HabitTracker.Tests/NotificationMessageFactoryTests.cs`

Что показать на ревью:
- текст уведомлений вынесен в отдельную фабрику;
- сервис notification jobs не собирает текст напрямую;
- фабрика тестируется отдельно.

### Коммит 2: Existing backend tests support
Файлы:
- `backend/tests/HabitTracker.Tests/HabitServiceTests.cs`
- `backend/tests/HabitTracker.Tests/StreakCalculatorTests.cs`
- `backend/tests/HabitTracker.Tests/HabitTracker.Tests.csproj`

Что показать на ревью:
- тесты проверяют бизнес-логику streak;
- тесты проверяют запрет повторного выполнения привычки за день;
- тестовый проект явно подключает зависимости.

## Общий коммит

Файлы:
- `.env.example`
- `README.md`
- `docs/review-commit-plan.md`
- `docker-compose.yml`

Что показать на ревью:
- реальные секреты не коммитятся;
- запуск описан через Docker Compose;
- есть отдельный документ с распределением работ;
- backend и Go запускаются как отдельные сервисы.
