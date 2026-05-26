package server

import (
	"encoding/json"
	"net/http"
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
	if err := s.telegramSender.Send(request.TelegramChatID, request.Text); err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]string{"status": "failed", "error": err.Error()})
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
	if err := s.emailSender.Send(request.Email, request.Subject, request.Text); err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]string{"status": "failed", "error": err.Error()})
		return
	}
	writeJSON(w, http.StatusOK, map[string]string{"status": "sent"})
}

func decodeJSON(w http.ResponseWriter, r *http.Request, target any) bool {
	defer r.Body.Close()
	if err := json.NewDecoder(r.Body).Decode(target); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid_json", "message": err.Error()})
		return false
	}
	return true
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}
