package notifications

import (
	"context"
	"fmt"
	"log"
	"time"

	"habittracker-go/internal/habitapi"
)

type APIClient interface {
	GetDueNotifications(ctx context.Context, before time.Time) ([]habitapi.NotificationJob, error)
	MarkNotificationSent(ctx context.Context, jobID string) error
	MarkNotificationFailed(ctx context.Context, jobID string, reason string) error
}

type TelegramSender interface {
	Send(chatID string, text string) error
}

type EmailSender interface {
	Send(to string, subject string, text string) error
}

type Worker struct {
	apiClient      APIClient
	telegramSender TelegramSender
	emailSender    EmailSender
	interval       time.Duration
}

func NewWorker(apiClient APIClient, telegramSender TelegramSender, emailSender EmailSender, interval time.Duration) *Worker {
	return &Worker{
		apiClient:      apiClient,
		telegramSender: telegramSender,
		emailSender:    emailSender,
		interval:       interval,
	}
}

func (w *Worker) Start(ctx context.Context) {
	if w.interval <= 0 {
		w.interval = time.Minute
	}

	ticker := time.NewTicker(w.interval)
	defer ticker.Stop()

	w.process(ctx)
	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			w.process(ctx)
		}
	}
}

func (w *Worker) process(ctx context.Context) {
	jobs, err := w.apiClient.GetDueNotifications(ctx, time.Now().UTC())
	if err != nil {
		log.Printf("notification polling failed: %v", err)
		return
	}

	for _, job := range jobs {
		if err := w.send(job); err != nil {
			log.Printf("notification %s failed: %v", job.ID, err)
			_ = w.apiClient.MarkNotificationFailed(ctx, job.ID, err.Error())
			continue
		}
		_ = w.apiClient.MarkNotificationSent(ctx, job.ID)
	}
}

func (w *Worker) send(job habitapi.NotificationJob) error {
	switch job.Channel {
	case "telegram":
		return w.telegramSender.Send(job.Recipient, job.Text)
	case "email":
		return w.emailSender.Send(job.Recipient, "Habit Tracker Reminder", job.Text)
	default:
		return fmt.Errorf("unsupported notification channel: %s", job.Channel)
	}
}
