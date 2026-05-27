package telegram

import "testing"

func TestSplitCommand(t *testing.T) {
	command, arg := splitCommand("/done 12")
	if command != "/done" {
		t.Fatalf("command = %q", command)
	}
	if arg != "12" {
		t.Fatalf("arg = %q", arg)
	}
}
