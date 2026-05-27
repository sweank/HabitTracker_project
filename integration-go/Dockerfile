FROM golang:1.23 AS build
WORKDIR /src
COPY . .
RUN go test ./...
RUN CGO_ENABLED=0 GOOS=linux go build -o /app/bot ./cmd/bot

FROM alpine:3.20
WORKDIR /app
COPY --from=build /app/bot .
EXPOSE 8081
ENTRYPOINT ["/app/bot"]
