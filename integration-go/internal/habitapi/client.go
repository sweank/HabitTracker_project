package habitapi

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strings"
	"time"
)

type HTTPDoer interface {
	Do(req *http.Request) (*http.Response, error)
}

type ClientOption func(*Client)

type Client struct {
	baseURL    string
	httpClient HTTPDoer
}

func NewClient(baseURL string, options ...ClientOption) *Client {
	client := &Client{
		baseURL: strings.TrimRight(baseURL, "/"),
		httpClient: &http.Client{
			Timeout: 10 * time.Second,
		},
	}

	for _, option := range options {
		option(client)
	}

	return client
}

func WithHTTPClient(httpClient HTTPDoer) ClientOption {
	return func(client *Client) {
		if httpClient != nil {
			client.httpClient = httpClient
		}
	}
}

func (c *Client) GetUserByTelegramID(ctx context.Context, telegramChatID string) (*User, error) {
	var result User
	err := c.do(ctx, http.MethodGet, "/api/users/by-telegram/"+url.PathEscape(telegramChatID), nil, &result)
	if err != nil {
		return nil, err
	}
	return &result, nil
}

func (c *Client) CreateUser(ctx context.Context, request CreateUserRequest) (*User, error) {
	var result User
	err := c.do(ctx, http.MethodPost, "/api/users", request, &result)
	if err != nil {
		return nil, err
	}
	return &result, nil
}

func (c *Client) CreateHabit(ctx context.Context, request CreateHabitRequest) (*CreateHabitResponse, error) {
	var result CreateHabitResponse
	err := c.do(ctx, http.MethodPost, "/api/habits", request, &result)
	if err != nil {
		return nil, err
	}
	return &result, nil
}

func (c *Client) GetUserHabits(ctx context.Context, userID string) ([]Habit, error) {
	var result []Habit
	err := c.do(ctx, http.MethodGet, "/api/users/"+url.PathEscape(userID)+"/habits", nil, &result)
	return result, err
}

func (c *Client) CompleteHabit(ctx context.Context, habitID string, date string) (*CompleteHabitResponse, error) {
	var result CompleteHabitResponse
	req := CompleteHabitRequest{Date: date}
	err := c.do(ctx, http.MethodPost, "/api/habits/"+url.PathEscape(habitID)+"/completions", req, &result)
	if err != nil {
		return nil, err
	}
	return &result, nil
}

func (c *Client) GetHabitStats(ctx context.Context, habitID string) (*HabitStats, error) {
	var result HabitStats
	err := c.do(ctx, http.MethodGet, "/api/habits/"+url.PathEscape(habitID)+"/stats", nil, &result)
	if err != nil {
		return nil, err
	}
	return &result, nil
}

func (c *Client) GetDueNotifications(ctx context.Context, before time.Time) ([]NotificationJob, error) {
	path := "/api/notification-jobs/due?before=" + url.QueryEscape(before.UTC().Format(time.RFC3339))
	var result []NotificationJob
	err := c.do(ctx, http.MethodGet, path, nil, &result)
	return result, err
}

func (c *Client) MarkNotificationSent(ctx context.Context, jobID string) error {
	return c.do(ctx, http.MethodPost, "/api/notification-jobs/"+url.PathEscape(jobID)+"/sent", nil, nil)
}

func (c *Client) MarkNotificationFailed(ctx context.Context, jobID string, reason string) error {
	body := map[string]string{"reason": reason}
	return c.do(ctx, http.MethodPost, "/api/notification-jobs/"+url.PathEscape(jobID)+"/failed", body, nil)
}

func (c *Client) do(ctx context.Context, method string, path string, requestBody any, responseBody any) error {
	if strings.TrimSpace(c.baseURL) == "" {
		return fmt.Errorf("backend base url is empty")
	}

	var body io.Reader
	if requestBody != nil {
		payload, err := json.Marshal(requestBody)
		if err != nil {
			return fmt.Errorf("marshal request: %w", err)
		}
		body = bytes.NewReader(payload)
	}

	req, err := http.NewRequestWithContext(ctx, method, c.baseURL+path, body)
	if err != nil {
		return fmt.Errorf("build backend request: %w", err)
	}
	if requestBody != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	req.Header.Set("Accept", "application/json")

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("backend request failed: %w", err)
	}
	defer resp.Body.Close()

	data, err := io.ReadAll(resp.Body)
	if err != nil {
		return fmt.Errorf("read backend response: %w", err)
	}

	if resp.StatusCode >= 400 {
		var apiErr APIError
		if err := json.Unmarshal(data, &apiErr); err == nil && apiErr.Message != "" {
			apiErr.StatusCode = resp.StatusCode
			return apiErr
		}
		return fmt.Errorf("backend returned %s: %s", resp.Status, strings.TrimSpace(string(data)))
	}

	if responseBody == nil || len(data) == 0 {
		return nil
	}
	if err := json.Unmarshal(data, responseBody); err != nil {
		return fmt.Errorf("decode backend response: %w", err)
	}
	return nil
}
