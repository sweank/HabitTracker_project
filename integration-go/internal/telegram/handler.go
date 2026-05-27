package telegram

import (
	"context"
	"fmt"
	"strconv"
	"strings"
	"time"

	"habittracker-go/internal/habitapi"
)

type HabitAPI interface {
	GetUserByTelegramID(ctx context.Context, telegramChatID string) (*habitapi.User, error)
	CreateUser(ctx context.Context, request habitapi.CreateUserRequest) (*habitapi.User, error)
	CreateHabit(ctx context.Context, request habitapi.CreateHabitRequest) (*habitapi.CreateHabitResponse, error)
	GetUserHabits(ctx context.Context, userID string) ([]habitapi.Habit, error)
	CompleteHabit(ctx context.Context, habitID string, date string) (*habitapi.CompleteHabitResponse, error)
	GetHabitStats(ctx context.Context, habitID string) (*habitapi.HabitStats, error)
}

type CommandHandler struct {
	api HabitAPI
}

func NewCommandHandler(api HabitAPI) *CommandHandler {
	return &CommandHandler{api: api}
}

func (h *CommandHandler) Handle(ctx context.Context, message Message) string {
	chatID := strconv.FormatInt(message.ChatID, 10)
	text := strings.TrimSpace(message.Text)

	command, arg := splitCommand(text)
	switch command {
	case "/start":
		return h.handleStart(ctx, chatID, message.FromName)
	case "/habits":
		return h.handleHabits(ctx, chatID)
	case "/done":
		return h.handleDone(ctx, chatID, arg)
	case "/stats":
		return h.handleStats(ctx, chatID)
	case "/new":
		return h.handleNew(ctx, chatID, arg)
	default:
		return "Команды: /start, /habits, /done, /done 1, /stats, /new Drink water"
	}
}

func (h *CommandHandler) handleStart(ctx context.Context, chatID string, name string) string {
	user, err := h.api.GetUserByTelegramID(ctx, chatID)
	if err == nil {
		return "Ты уже зарегистрирован: " + user.Name
	}

	if strings.TrimSpace(name) == "" {
		name = "Telegram User"
	}

	user, err = h.api.CreateUser(ctx, habitapi.CreateUserRequest{
		Name:           name,
		Email:          "telegram" + chatID + "@example.local",
		TelegramChatID: chatID,
	})
	if err != nil {
		return "Не удалось зарегистрировать пользователя: " + err.Error()
	}

	return "Готово, пользователь создан: " + user.Name
}

func (h *CommandHandler) handleHabits(ctx context.Context, chatID string) string {
	user, habits, err := h.getUserAndHabits(ctx, chatID)
	_ = user
	if err != nil {
		return err.Error()
	}
	if len(habits) == 0 {
		return "Привычек пока нет. Создай привычку: /new Drink water"
	}
	return formatHabits(habits)
}

func (h *CommandHandler) handleDone(ctx context.Context, chatID string, arg string) string {
	_, habits, err := h.getUserAndHabits(ctx, chatID)
	if err != nil {
		return err.Error()
	}
	if len(habits) == 0 {
		return "Привычек пока нет. Создай привычку: /new Drink water"
	}

	if strings.TrimSpace(arg) == "" {
		return "Выбери номер привычки:\n" + formatHabits(habits) + "\nНапример: /done 1"
	}

	index, err := strconv.Atoi(strings.TrimSpace(arg))
	if err != nil || index < 1 || index > len(habits) {
		return "Неверный номер привычки. Напиши /done, чтобы увидеть список."
	}

	date := time.Now().Format("2006-01-02")
	result, err := h.api.CompleteHabit(ctx, habits[index-1].ID, date)
	if err != nil {
		return "Не удалось отметить привычку: " + err.Error()
	}

	return fmt.Sprintf("Готово: %s. Текущий streak: %d", habits[index-1].Title, result.CurrentStreak)
}

func (h *CommandHandler) handleStats(ctx context.Context, chatID string) string {
	_, habits, err := h.getUserAndHabits(ctx, chatID)
	if err != nil {
		return err.Error()
	}
	if len(habits) == 0 {
		return "Привычек пока нет."
	}

	var builder strings.Builder
	builder.WriteString("Статистика:\n")
	for i, habit := range habits {
		stats, err := h.api.GetHabitStats(ctx, habit.ID)
		if err != nil {
			builder.WriteString(fmt.Sprintf("%d. %s — ошибка статистики: %v\n", i+1, habit.Title, err))
			continue
		}
		builder.WriteString(fmt.Sprintf("%d. %s — current: %d, best: %d\n", i+1, habit.Title, stats.CurrentStreak, stats.BestStreak))
	}
	return strings.TrimSpace(builder.String())
}

func (h *CommandHandler) handleNew(ctx context.Context, chatID string, title string) string {
	if strings.TrimSpace(title) == "" {
		return "Напиши название привычки. Например: /new Drink water"
	}

	user, err := h.api.GetUserByTelegramID(ctx, chatID)
	if err != nil {
		return "Сначала зарегистрируйся: /start"
	}

	_, err = h.api.CreateHabit(ctx, habitapi.CreateHabitRequest{
		UserID:           user.ID,
		Title:            strings.TrimSpace(title),
		Description:      "Created from Telegram",
		ReminderTime:     "09:00",
		NotifyInTelegram: true,
		NotifyByEmail:    false,
	})
	if err != nil {
		return "Не удалось создать привычку: " + err.Error()
	}
	return "Привычка создана: " + strings.TrimSpace(title)
}

func (h *CommandHandler) getUserAndHabits(ctx context.Context, chatID string) (*habitapi.User, []habitapi.Habit, error) {
	user, err := h.api.GetUserByTelegramID(ctx, chatID)
	if err != nil {
		return nil, nil, fmt.Errorf("Сначала зарегистрируйся: /start")
	}
	habits, err := h.api.GetUserHabits(ctx, user.ID)
	if err != nil {
		return nil, nil, fmt.Errorf("Не удалось получить привычки: %w", err)
	}
	return user, habits, nil
}

func formatHabits(habits []habitapi.Habit) string {
	var builder strings.Builder
	for i, habit := range habits {
		status := "не выполнено сегодня"
		if habit.IsCompletedToday {
			status = "выполнено сегодня"
		}
		builder.WriteString(fmt.Sprintf("%d. %s — streak: %d, %s\n", i+1, habit.Title, habit.CurrentStreak, status))
	}
	return strings.TrimSpace(builder.String())
}

func splitCommand(text string) (string, string) {
	parts := strings.SplitN(text, " ", 2)
	command := strings.ToLower(strings.TrimSpace(parts[0]))
	if len(parts) == 1 {
		return command, ""
	}
	return command, strings.TrimSpace(parts[1])
}
