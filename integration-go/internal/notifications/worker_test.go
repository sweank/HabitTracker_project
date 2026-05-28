package notifications

import (
	"errors"
	"testing"

	"habittracker-go/internal/habitapi"
)

type fakeTelegram struct{ called bool }

func (f *fakeTelegram) Send(chatID string, text string) error { f.called = true; return nil }

type fakeEmail struct{ called bool }

func (f *fakeEmail) Send(to string, subject string, text string) error { f.called = true; return nil }

func TestWorkerSendChoosesTelegramChannel(t *testing.T) {
	telegram := &fakeTelegram{}
	email := &fakeEmail{}
	worker := NewWorker(nil, telegram, email, 0)

	err := worker.send(habitapi.NotificationJob{Channel: "telegram", Recipient: "1", Text: "hello"})
	if err != nil {
		t.Fatal(err)
	}
	if !telegram.called || email.called {
		t.Fatalf("telegram called=%v email called=%v", telegram.called, email.called)
	}
}

func TestWorkerSendRejectsUnknownChannel(t *testing.T) {
	worker := NewWorker(nil, &fakeTelegram{}, &fakeEmail{}, 0)
	err := worker.send(habitapi.NotificationJob{Channel: "push"})
	if err == nil || errors.Is(err, nil) {
		t.Fatal("expected error")
	}
}
