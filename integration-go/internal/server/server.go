package server

import (
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"strings"
)

type TelegramSender interface {
	Send(chatID string, text string) error
}

type EmailSender interface {
	Send(to string, subject string, text string) error
}

type Server struct {
	addr           string
	telegramSender TelegramSender
	emailSender    EmailSender
}

func New(addr string, telegramSender TelegramSender, emailSender EmailSender) *http.Server {
	server := &Server{addr: addr, telegramSender: telegramSender, emailSender: emailSender}
	mux := http.NewServeMux()
	mux.HandleFunc("GET /health", server.health)
	mux.HandleFunc("POST /api/notifications/telegram", server.sendTelegram)
	mux.HandleFunc("POST /api/notifications/email", server.sendEmail)
	return &http.Server{Addr: addr, Handler: mux}
}

func (s *Server) health(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
}

func (s *Server) sendTelegram(w http.ResponseWriter, r *http.Request) {
	var request struct {
		TelegramChatID string `json:"telegramChatId"`
		Text           string `json:"text"`
	}
	if !decodeJSON(w, r, &request) {
		return
	}
	if err := requireFields(map[string]string{
		"telegramChatId": request.TelegramChatID,
		"text":           request.Text,
	}); err != nil {
		writeError(w, http.StatusBadRequest, "validation_error", err.Error())
		return
	}
	if err := s.telegramSender.Send(request.TelegramChatID, request.Text); err != nil {
		writeError(w, http.StatusBadGateway, "telegram_send_failed", err.Error())
		return
	}
	writeJSON(w, http.StatusOK, map[string]string{"status": "sent"})
}

func (s *Server) sendEmail(w http.ResponseWriter, r *http.Request) {
	var request struct {
		Email   string `json:"email"`
		Subject string `json:"subject"`
		Text    string `json:"text"`
	}
	if !decodeJSON(w, r, &request) {
		return
	}
	if err := requireFields(map[string]string{
		"email":   request.Email,
		"subject": request.Subject,
		"text":    request.Text,
	}); err != nil {
		writeError(w, http.StatusBadRequest, "validation_error", err.Error())
		return
	}
	if err := s.emailSender.Send(request.Email, request.Subject, request.Text); err != nil {
		writeError(w, http.StatusBadGateway, "email_send_failed", err.Error())
		return
	}
	writeJSON(w, http.StatusOK, map[string]string{"status": "sent"})
}

func decodeJSON(w http.ResponseWriter, r *http.Request, target any) bool {
	defer r.Body.Close()
	r.Body = http.MaxBytesReader(w, r.Body, 1<<20)
	decoder := json.NewDecoder(r.Body)
	decoder.DisallowUnknownFields()
	if err := decoder.Decode(target); err != nil {
		writeError(w, http.StatusBadRequest, "invalid_json", err.Error())
		return false
	}
	return true
}

func requireFields(fields map[string]string) error {
	var missing []string
	for field, value := range fields {
		if strings.TrimSpace(value) == "" {
			missing = append(missing, field)
		}
	}
	if len(missing) > 0 {
		return fmt.Errorf("required fields are empty: %s", strings.Join(missing, ", "))
	}
	return nil
}

func writeError(w http.ResponseWriter, status int, code string, message string) {
	if strings.TrimSpace(message) == "" {
		message = errors.New("unknown error").Error()
	}
	writeJSON(w, status, map[string]string{"errorCode": code, "message": message})
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}
