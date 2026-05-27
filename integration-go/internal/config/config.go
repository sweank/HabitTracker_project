package config

import (
	"os"
	"strconv"
	"time"
)

type Config struct {
	BackendURL       string
	TelegramBotToken string
	HTTPAddr         string
	PollInterval     time.Duration
	SMTPHost         string
	SMTPPort         string
	SMTPUsername     string
	SMTPPassword     string
	EmailFrom        string
}

func Load() Config {
	pollSeconds := getInt("POLL_INTERVAL_SECONDS", 60)
	return Config{
		BackendURL:       getString("BACKEND_URL", "http://localhost:5000"),
		TelegramBotToken: getString("TELEGRAM_BOT_TOKEN", ""),
		HTTPAddr:         getString("GO_HTTP_ADDR", ":8081"),
		PollInterval:     time.Duration(pollSeconds) * time.Second,
		SMTPHost:         getString("SMTP_HOST", ""),
		SMTPPort:         getString("SMTP_PORT", "587"),
		SMTPUsername:     getString("SMTP_USERNAME", ""),
		SMTPPassword:     getString("SMTP_PASSWORD", ""),
		EmailFrom:        getString("EMAIL_FROM", "habit-tracker@example.local"),
	}
}

func getString(key string, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	return value
}

func getInt(key string, fallback int) int {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	parsed, err := strconv.Atoi(value)
	if err != nil {
		return fallback
	}
	return parsed
}
