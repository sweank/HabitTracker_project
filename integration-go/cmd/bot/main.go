package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"

	"habittracker-go/internal/config"
	mail "habittracker-go/internal/email"
	"habittracker-go/internal/habitapi"
	"habittracker-go/internal/notifications"
	"habittracker-go/internal/server"
	"habittracker-go/internal/telegram"
)

func main() {
	cfg := config.Load()
	if err := cfg.Validate(); err != nil {
		log.Fatalf("invalid config: %v", err)
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	apiClient := habitapi.NewClient(cfg.BackendURL)
	emailSender := mail.NewSender(cfg)
	bot := telegram.NewBot(cfg.TelegramBotToken)
	commandHandler := telegram.NewCommandHandler(apiClient)
	worker := notifications.NewWorker(apiClient, bot, emailSender, cfg.PollInterval)
	apiServer := server.New(cfg.HTTPAddr, bot, emailSender)

	go worker.Start(ctx)

	go func() {
		log.Printf("go http service listening on %s", cfg.HTTPAddr)
		if err := apiServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Printf("go http service error: %v", err)
		}
	}()

	if cfg.TelegramBotToken == "" {
		log.Print("TELEGRAM_BOT_TOKEN is empty; telegram polling is disabled, notification worker and HTTP endpoints are still running")
		<-ctx.Done()
		return
	}

	log.Print("telegram bot polling started")
	if err := bot.Start(ctx, commandHandler.Handle); err != nil {
		log.Printf("telegram bot stopped with error: %v", err)
	}
}
