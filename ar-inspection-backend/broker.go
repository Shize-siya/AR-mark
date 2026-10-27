package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"sync"
	"time"
)

// Broker 管理 SSE 订阅者，每次 Issue 增删改后广播 JSON event
type Broker struct {
	mu      sync.Mutex
	clients map[chan SSEEvent]bool
	history []SSEEvent // 保留最近 50 条，新订阅者可立即收到
}

func NewBroker() *Broker {
	return &Broker{
		clients: make(map[chan SSEEvent]bool),
		history: make([]SSEEvent, 0, 50),
	}
}

func (b *Broker) Subscribe() chan SSEEvent {
	b.mu.Lock()
	defer b.mu.Unlock()
	ch := make(chan SSEEvent, 16)
	b.clients[ch] = true
	// 把历史事件先推给新订阅者
	for _, evt := range b.history {
		select {
		case ch <- evt:
		default: // channel 满了就跳过，不阻塞
		}
	}
	return ch
}

func (b *Broker) Unsubscribe(ch chan SSEEvent) {
	b.mu.Lock()
	defer b.mu.Unlock()
	delete(b.clients, ch)
	close(ch)
}

func (b *Broker) Publish(evt SSEEvent) {
	b.mu.Lock()
	// 加入历史
	b.history = append(b.history, evt)
	if len(b.history) > 50 {
		b.history = b.history[len(b.history)-50:]
	}
	clients := make([]chan SSEEvent, 0, len(b.clients))
	for ch := range b.clients {
		clients = append(clients, ch)
	}
	b.mu.Unlock()

	// 非阻塞广播，慢消费者不会拖垮
	for _, ch := range clients {
		select {
		case ch <- evt:
		default:
		}
	}
}

// ===== GET /api/events — SSE 端点 =====

func (h *Handlers) Events(w http.ResponseWriter, r *http.Request) {
	flusher, ok := w.(http.Flusher)
	if !ok {
		http.Error(w, "Streaming unsupported", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "text/event-stream")
	w.Header().Set("Cache-Control", "no-cache")
	w.Header().Set("Connection", "keep-alive")

	ch := h.store.Broker().Subscribe()
	defer h.store.Broker().Unsubscribe(ch)

	// 心跳：每 25 秒发一条 :ping\n\n 防止代理断连
	ticker := time.NewTicker(25 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-r.Context().Done():
			return
		case evt := <-ch:
			data, _ := json.Marshal(evt)
			fmt.Fprintf(w, "event: %s\n", evt.Type)
			fmt.Fprintf(w, "data: %s\n\n", data)
			flusher.Flush()
		case <-ticker.C:
			fmt.Fprint(w, ":ping\n\n")
			flusher.Flush()
		}
	}
}
