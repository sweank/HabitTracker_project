package habitapi

type User struct {
	ID             string `json:"id"`
	Name           string `json:"name"`
	Email          string `json:"email"`
	TelegramChatID string `json:"telegramChatId"`
}

type CreateUserRequest struct {
	Name           string `json:"name"`
	Email          string `json:"email"`
	TelegramChatID string `json:"telegramChatId"`
}

type Habit struct {
	ID               string `json:"id"`
	Title            string `json:"title"`
	Description      string `json:"description"`
	CurrentStreak    int    `json:"currentStreak"`
	IsCompletedToday bool   `json:"isCompletedToday"`
}

type CreateHabitRequest struct {
	UserID           string `json:"userId"`
	Title            string `json:"title"`
	Description      string `json:"description"`
	ReminderTime     string `json:"reminderTime"`
	NotifyInTelegram bool   `json:"notifyInTelegram"`
	NotifyByEmail    bool   `json:"notifyByEmail"`
}

type CreateHabitResponse struct {
	ID string `json:"id"`
}

type CompleteHabitRequest struct {
	Date string `json:"date,omitempty"`
}

type CompleteHabitResponse struct {
	HabitID       string `json:"habitId"`
	Date          string `json:"date"`
	CurrentStreak int    `json:"currentStreak"`
}

type HabitStats struct {
	HabitID        string   `json:"habitId"`
	CurrentStreak  int      `json:"currentStreak"`
	BestStreak     int      `json:"bestStreak"`
	CompletedDates []string `json:"completedDates"`
}

type NotificationJob struct {
	ID        string `json:"id"`
	UserID    string `json:"userId"`
	HabitID   string `json:"habitId"`
	Channel   string `json:"channel"`
	Recipient string `json:"recipient"`
	Text      string `json:"text"`
}

type APIError struct {
	ErrorCode string `json:"errorCode"`
	Message   string `json:"message"`
}

func (e APIError) Error() string {
	if e.ErrorCode == "" {
		return e.Message
	}
	return e.ErrorCode + ": " + e.Message
}
