package telegram

import (
	"context"
	"errors"
	"strings"
	"testing"

	"habittracker-go/internal/habitapi"
)

func TestSplitCommand(t *testing.T) {
	command, arg := splitCommand("/done 12")
	if command != "/done" {
		t.Fatalf("command = %q", command)
	}
	if arg != "12" {
		t.Fatalf("arg = %q", arg)
	}
}

func TestHandleStartCreatesUserWhenMissing(t *testing.T) {
	api := &fakeHabitAPI{getUserErr: errors.New("not found")}
	handler := NewCommandHandler(api)

	answer := handler.Handle(context.Background(), Message{ChatID: 123, Text: "/start", FromName: "Ivan"})

	if !strings.Contains(answer, "пользователь создан") {
		t.Fatalf("answer = %q", answer)
	}
	if !api.createUserCalled {
		t.Fatal("CreateUser should be called")
	}
}

func TestHandleNewRequiresRegistration(t *testing.T) {
	api := &fakeHabitAPI{getUserErr: errors.New("not found")}
	handler := NewCommandHandler(api)

	answer := handler.Handle(context.Background(), Message{ChatID: 123, Text: "/new Water"})

	if !strings.Contains(answer, "/start") {
		t.Fatalf("answer = %q", answer)
	}
	if api.createHabitCalled {
		t.Fatal("CreateHabit should not be called")
	}
}

type fakeHabitAPI struct {
	getUserErr        error
	createUserCalled  bool
	createHabitCalled bool
}

func (f *fakeHabitAPI) GetUserByTelegramID(ctx context.Context, telegramChatID string) (*habitapi.User, error) {
	if f.getUserErr != nil {
		return nil, f.getUserErr
	}
	return &habitapi.User{ID: "user-1", Name: "Ivan", Email: "ivan@mail.com", TelegramChatID: telegramChatID}, nil
}

func (f *fakeHabitAPI) CreateUser(ctx context.Context, request habitapi.CreateUserRequest) (*habitapi.User, error) {
	f.createUserCalled = true
	return &habitapi.User{ID: "user-1", Name: request.Name, Email: request.Email, TelegramChatID: request.TelegramChatID}, nil
}

func (f *fakeHabitAPI) CreateHabit(ctx context.Context, request habitapi.CreateHabitRequest) (*habitapi.CreateHabitResponse, error) {
	f.createHabitCalled = true
	return &habitapi.CreateHabitResponse{ID: "habit-1"}, nil
}

func (f *fakeHabitAPI) GetUserHabits(ctx context.Context, userID string) ([]habitapi.Habit, error) {
	return []habitapi.Habit{{ID: "habit-1", Title: "Water"}}, nil
}

func (f *fakeHabitAPI) CompleteHabit(ctx context.Context, habitID string, date string) (*habitapi.CompleteHabitResponse, error) {
	return &habitapi.CompleteHabitResponse{HabitID: habitID, Date: date, CurrentStreak: 1}, nil
}

func (f *fakeHabitAPI) GetHabitStats(ctx context.Context, habitID string) (*habitapi.HabitStats, error) {
	return &habitapi.HabitStats{HabitID: habitID, CurrentStreak: 1, BestStreak: 1}, nil
}
