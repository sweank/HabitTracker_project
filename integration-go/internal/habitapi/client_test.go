package habitapi

import (
	"context"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestClientCreateUserSendsJSON(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			t.Fatalf("method = %s", r.Method)
		}
		if r.URL.Path != "/api/users" {
			t.Fatalf("path = %s", r.URL.Path)
		}
		if r.Header.Get("Content-Type") != "application/json" {
			t.Fatalf("content-type = %s", r.Header.Get("Content-Type"))
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"id":"u1","name":"Ivan","email":"ivan@mail.com","telegramChatId":"123"}`))
	}))
	defer server.Close()

	client := NewClient(server.URL)
	user, err := client.CreateUser(context.Background(), CreateUserRequest{Name: "Ivan", Email: "ivan@mail.com", TelegramChatID: "123"})
	if err != nil {
		t.Fatal(err)
	}
	if user.Name != "Ivan" || user.TelegramChatID != "123" {
		t.Fatalf("unexpected user: %#v", user)
	}
}

func TestClientReturnsAPIError(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusBadRequest)
		_, _ = w.Write([]byte(`{"errorCode":"VALIDATION_ERROR","message":"Name is required","traceId":"trace-1"}`))
	}))
	defer server.Close()

	client := NewClient(server.URL)
	_, err := client.GetUserByTelegramID(context.Background(), "123")
	var apiErr APIError
	if !errors.As(err, &apiErr) {
		t.Fatalf("expected APIError, got %T: %v", err, err)
	}
	if apiErr.StatusCode != http.StatusBadRequest || apiErr.TraceID != "trace-1" {
		t.Fatalf("unexpected api error: %#v", apiErr)
	}
}
