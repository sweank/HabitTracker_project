package config

import (
	"testing"
	"time"
)

func TestConfigValidateRejectsMissingBackendURL(t *testing.T) {
	cfg := Config{BackendURL: "", HTTPAddr: ":8081", PollInterval: time.Second}
	if err := cfg.Validate(); err == nil {
		t.Fatal("expected validation error")
	}
}

func TestConfigValidateAcceptsMinimalConfig(t *testing.T) {
	cfg := Config{BackendURL: "http://localhost:5000", HTTPAddr: ":8081", PollInterval: time.Second}
	if err := cfg.Validate(); err != nil {
		t.Fatal(err)
	}
}
