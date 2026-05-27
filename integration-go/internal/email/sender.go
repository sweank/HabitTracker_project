package email

import (
	"fmt"
	"net/smtp"
	"strings"

	"habittracker-go/internal/config"
)

type Sender struct {
	host     string
	port     string
	username string
	password string
	from     string
}

func NewSender(cfg config.Config) *Sender {
	return &Sender{
		host:     cfg.SMTPHost,
		port:     cfg.SMTPPort,
		username: cfg.SMTPUsername,
		password: cfg.SMTPPassword,
		from:     cfg.EmailFrom,
	}
}

func (s *Sender) Send(to string, subject string, text string) error {
	if s.host == "" || s.username == "" || s.password == "" {
		fmt.Printf("[email disabled] to=%s subject=%s text=%s\n", to, subject, text)
		return nil
	}

	addr := s.host + ":" + s.port
	auth := smtp.PlainAuth("", s.username, s.password, s.host)
	message := strings.Join([]string{
		"From: " + s.from,
		"To: " + to,
		"Subject: " + subject,
		"Content-Type: text/plain; charset=utf-8",
		"",
		text,
	}, "\r\n")

	return smtp.SendMail(addr, auth, s.from, []string{to}, []byte(message))
}
