package telegram

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strconv"
	"time"
)

type Bot struct {
	token      string
	httpClient *http.Client
}

func NewBot(token string) *Bot {
	return &Bot{
		token:      token,
		httpClient: &http.Client{Timeout: 30 * time.Second},
	}
}

func (b *Bot) Send(chatID string, text string) error {
	if b.token == "" {
		fmt.Printf("[telegram disabled] chat=%s text=%s\n", chatID, text)
		return nil
	}

	parsed, err := strconv.ParseInt(chatID, 10, 64)
	if err != nil {
		return fmt.Errorf("invalid telegram chat id: %w", err)
	}
	return b.SendToChatID(parsed, text)
}

func (b *Bot) SendToChatID(chatID int64, text string) error {
	if b.token == "" {
		fmt.Printf("[telegram disabled] chat=%d text=%s\n", chatID, text)
		return nil
	}

	payload, err := json.Marshal(outboundMessage{ChatID: chatID, Text: text})
	if err != nil {
		return err
	}

	endpoint := b.apiURL("sendMessage")
	resp, err := b.httpClient.Post(endpoint, "application/json", bytes.NewReader(payload))
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 400 {
		data, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("telegram sendMessage failed: %s: %s", resp.Status, string(data))
	}
	return nil
}

func (b *Bot) Start(ctx context.Context, handler func(context.Context, Message) string) error {
	if b.token == "" {
		return errors.New("telegram bot token is empty")
	}

	offset := 0
	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}

		updates, err := b.getUpdates(ctx, offset)
		if err != nil {
			time.Sleep(2 * time.Second)
			continue
		}

		for _, update := range updates.Result {
			offset = update.UpdateID + 1
			if update.Message.Text == "" {
				continue
			}

			fromName := update.Message.From.FirstName
			if fromName == "" {
				fromName = update.Message.From.Username
			}

			message := Message{
				ChatID:   update.Message.Chat.ID,
				Text:     update.Message.Text,
				FromName: fromName,
			}

			answer := handler(ctx, message)
			if answer != "" {
				_ = b.SendToChatID(message.ChatID, answer)
			}
		}
	}
}

func (b *Bot) getUpdates(ctx context.Context, offset int) (*getUpdatesResponse, error) {
	params := url.Values{}
	params.Set("timeout", "25")
	params.Set("offset", strconv.Itoa(offset))

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, b.apiURL("getUpdates")+"?"+params.Encode(), nil)
	if err != nil {
		return nil, err
	}

	resp, err := b.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 400 {
		data, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("telegram getUpdates failed: %s: %s", resp.Status, string(data))
	}

	var result getUpdatesResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	return &result, nil
}

func (b *Bot) apiURL(method string) string {
	return "https://api.telegram.org/bot" + b.token + "/" + method
}
