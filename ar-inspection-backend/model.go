package main

import (
	"encoding/json"
	"errors"
	"net/http"
	"strings"
	"time"
)

// ===== 枚举定义 =====

type Priority string

const (
	PriorityLow    Priority = "low"
	PriorityMedium Priority = "medium"
	PriorityHigh   Priority = "high"
)

func (p Priority) Valid() bool {
	switch p {
	case PriorityLow, PriorityMedium, PriorityHigh:
		return true
	}
	return false
}

type Status string

const (
	StatusOpen       Status = "open"
	StatusInProgress Status = "in_progress"
	StatusResolved   Status = "resolved"
)

func (s Status) Valid() bool {
	switch s {
	case StatusOpen, StatusInProgress, StatusResolved:
		return true
	}
	return false
}

// ===== 用户与角色 =====

type Role string

const (
	RoleAdmin     Role = "admin"
	RoleInspector Role = "inspector"
	RoleViewer    Role = "viewer"
)

func (r Role) Valid() bool {
	switch r {
	case RoleAdmin, RoleInspector, RoleViewer:
		return true
	}
	return false
}

// User 存储在 users.json 中（密码哈希 + salt）
type User struct {
	ID           string    `json:"id"`
	Username     string    `json:"username"`
	PasswordHash string    `json:"passwordHash"` // base64(salt+hash)
	Role         Role      `json:"role"`
	CreatedAt    time.Time `json:"createdAt"`
}

// UserPublic 返回给前端的安全版本（不带密码）
type UserPublic struct {
	ID        string    `json:"id"`
	Username  string    `json:"username"`
	Role      Role      `json:"role"`
	CreatedAt time.Time `json:"createdAt"`
}

func (u User) Public() UserPublic {
	return UserPublic{ID: u.ID, Username: u.Username, Role: u.Role, CreatedAt: u.CreatedAt}
}

// ===== 认证请求/响应 =====

type RegisterRequest struct {
	Username string `json:"username"`
	Password string `json:"password"`
	Role     Role   `json:"role"` // 可选，不传默认 inspector
}

func (r *RegisterRequest) Validate() error {
	if strings.TrimSpace(r.Username) == "" {
		return errors.New("username 不能为空")
	}
	if len(r.Username) < 3 || len(r.Username) > 32 {
		return errors.New("username 长度必须在 3-32 之间")
	}
	if r.Password == "" {
		return errors.New("password 不能为空")
	}
	if len(r.Password) < 6 {
		return errors.New("password 至少 6 位")
	}
	if r.Role == "" {
		r.Role = RoleInspector
	}
	if !r.Role.Valid() {
		return errors.New("role 错误，可选值: admin / inspector / viewer")
	}
	return nil
}

type LoginRequest struct {
	Username string `json:"username"`
	Password string `json:"password"`
}

func (r *LoginRequest) Validate() error {
	if strings.TrimSpace(r.Username) == "" {
		return errors.New("username 不能为空")
	}
	if r.Password == "" {
		return errors.New("password 不能为空")
	}
	return nil
}

type LoginResponse struct {
	Token string     `json:"token"`
	User  UserPublic `json:"user"`
}

// ===== Issue 模型（增加 CreatedBy 字段）=====

type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

type Issue struct {
	ID          string    `json:"id"`
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    Priority  `json:"priority"`
	Status      Status    `json:"status"`
	Position    Position  `json:"position"`
	CreatedBy   string    `json:"createdBy,omitempty"` // 用户名
	CreatedAt   time.Time `json:"createdAt"`
	UpdatedAt   time.Time `json:"updatedAt"`
}

// CreateIssueRequest 是 Unity 上报问题时的请求体
type CreateIssueRequest struct {
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    Priority `json:"priority"`
	Position    Position `json:"position"`
}

func (r *CreateIssueRequest) Validate() error {
	if strings.TrimSpace(r.Title) == "" {
		return errors.New("title 为必填项，不能为空")
	}
	if !r.Priority.Valid() {
		return errors.New("priority 枚举值错误，可选值: low / medium / high")
	}
	return nil
}

type UpdateStatusRequest struct {
	Status Status `json:"status"`
}

func (r *UpdateStatusRequest) Validate() error {
	if !r.Status.Valid() {
		return errors.New("status 枚举值错误，可选值: open / in_progress / resolved")
	}
	return nil
}

// ===== SSE 事件 =====

type SSEEvent struct {
	Type string      `json:"type"` // issue.created / issue.updated / user.created / user.deleted
	Data interface{} `json:"data"`
	Time time.Time   `json:"time"`
}

// ===== 统一错误响应 =====

type ErrorResponse struct {
	Error   string `json:"error"`
	Message string `json:"message"`
}

func writeError(w http.ResponseWriter, code int, errType, msg string) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	json.NewEncoder(w).Encode(ErrorResponse{Error: errType, Message: msg})
}

func writeJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	json.NewEncoder(w).Encode(v)
}
