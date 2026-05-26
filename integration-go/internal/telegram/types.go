package telegram

type Message struct {
	ChatID   int64
	Text     string
	FromName string
}

type outboundMessage struct {
	ChatID int64  `json:"chat_id"`
	Text   string `json:"text"`
}

type getUpdatesResponse struct {
	OK     bool `json:"ok"`
	Result []struct {
		UpdateID int `json:"update_id"`
		Message  struct {
			Text string `json:"text"`
			Chat struct {
				ID int64 `json:"id"`
			} `json:"chat"`
			From struct {
				FirstName string `json:"first_name"`
				Username  string `json:"username"`
			} `json:"from"`
		} `json:"message"`
	} `json:"result"`
}
