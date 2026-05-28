package server

import (
	"bytes"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"
)

type fakeTelegramSender struct {
	called bool
	err    error
}

func (f *fakeTelegramSender) Send(chatID string, text string) error {
	f.called = true
	return f.err
}

type fakeEmailSender struct {
	called bool
	err    error
}

func (f *fakeEmailSender) Send(to string, subject string, text string) error {
	f.called = true
	return f.err
}

func TestHealthReturnsOK(t *testing.T) {
	server := New(":0", &fakeTelegramSender{}, &fakeEmailSender{})
	req := httptest.NewRequest(http.MethodGet, "/health", nil)
	res := httptest.NewRecorder()

	server.Handler.ServeHTTP(res, req)

	if res.Code != http.StatusOK {
		t.Fatalf("status = %d", res.Code)
	}
}

func TestSendTelegramRejectsInvalidPayload(t *testing.T) {
	sender := &fakeTelegramSender{}
	server := New(":0", sender, &fakeEmailSender{})
	req := httptest.NewRequest(http.MethodPost, "/api/notifications/telegram", bytes.NewBufferString(`{"text":"hello"}`))
	res := httptest.NewRecorder()

	server.Handler.ServeHTTP(res, req)

	if res.Code != http.StatusBadRequest {
		t.Fatalf("status = %d", res.Code)
	}
	if sender.called {
		t.Fatal("sender should not be called")
	}
}

func TestSendEmailReturnsBadGatewayWhenSenderFails(t *testing.T) {
	email := &fakeEmailSender{err: errors.New("smtp unavailable")}
	server := New(":0", &fakeTelegramSender{}, email)
	body := `{"email":"ivan@mail.com","subject":"s","text":"t"}`
	req := httptest.NewRequest(http.MethodPost, "/api/notifications/email", bytes.NewBufferString(body))
	res := httptest.NewRecorder()

	server.Handler.ServeHTTP(res, req)

	if res.Code != http.StatusBadGateway {
		t.Fatalf("status = %d", res.Code)
	}
	if !email.called {
		t.Fatal("email sender should be called")
	}
}
