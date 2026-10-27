package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"time"
)

func nowUTC() time.Time {
	return time.Now().UTC()
}

type Handlers struct {
	store *Store
}

func NewHandlers(store *Store) *Handlers {
	return &Handlers{store: store}
}

// ===== middleware =====

// authMiddleware 解析 Bearer token，把用户信息放进 request context
func authMiddleware(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		token := extractBearerToken(r)
		if token == "" {
			writeError(w, http.StatusUnauthorized, "NO_TOKEN", "缺少 Authorization: Bearer <token>")
			return
		}
		claims, err := verifyToken(token)
		if err != nil {
			writeError(w, http.StatusUnauthorized, "INVALID_TOKEN", err.Error())
			return
		}
		au := &AuthUser{ID: claims.Sub, Username: claims.Username, Role: claims.Role}
		ctx := context.WithValue(r.Context(), userCtxKey, au)
		next.ServeHTTP(w, r.WithContext(ctx))
	}
}

// requireRole 检查当前用户角色是否在 allowedRoles 中
func requireRole(next http.HandlerFunc, allowedRoles ...Role) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		au := r.Context().Value(userCtxKey).(*AuthUser)
		for _, ar := range allowedRoles {
			if au.Role == ar {
				next.ServeHTTP(w, r)
				return
			}
		}
		writeError(w, http.StatusForbidden, "FORBIDDEN", "权限不足")
	}
}

// ===== 未认证（Issue 接口保持向后兼容，但如果有 token 会关联用户）=====

func (h *Handlers) Health(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{
		"status":    "ok",
		"timestamp": nowUTC().Format(time.RFC3339),
	})
}

// ===== POST /api/issues =====

func (h *Handlers) CreateIssue(w http.ResponseWriter, r *http.Request) {
	var req CreateIssueRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if err := req.Validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", err.Error())
		return
	}

	now := nowUTC()
	issue := Issue{
		Title:       strings.TrimSpace(req.Title),
		Description: req.Description,
		Priority:    req.Priority,
		Status:      StatusOpen,
		Position:    req.Position,
		CreatedAt:   now,
		UpdatedAt:   now,
	}

	// 有 token 就记录是谁创建的
	if token := extractBearerToken(r); token != "" {
		if claims, err := verifyToken(token); err == nil {
			issue.CreatedBy = claims.Username
		}
	}

	created, err := h.store.AddIssue(issue)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "STORE_ERROR", "保存数据失败")
		return
	}
	writeJSON(w, http.StatusCreated, created)
}

// ===== GET /api/issues =====

func (h *Handlers) ListIssues(w http.ResponseWriter, r *http.Request) {
	status := Status(r.URL.Query().Get("status"))
	priority := Priority(r.URL.Query().Get("priority"))

	if status != "" && !status.Valid() {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", "status 枚举值错误")
		return
	}
	if priority != "" && !priority.Valid() {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", "priority 枚举值错误")
		return
	}
	writeJSON(w, http.StatusOK, h.store.ListIssues(status, priority))
}

// ===== GET /api/issues/{id} =====

func (h *Handlers) GetIssue(w http.ResponseWriter, r *http.Request, id string) {
	issue, found := h.store.GetIssueByID(id)
	if !found {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "问题不存在")
		return
	}
	writeJSON(w, http.StatusOK, issue)
}

// ===== PATCH /api/issues/{id}/status =====

func (h *Handlers) UpdateIssueStatus(w http.ResponseWriter, r *http.Request, id string) {
	var req UpdateStatusRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if err := req.Validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", err.Error())
		return
	}

	updated, found, err := h.store.UpdateIssueStatus(id, req.Status)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "STORE_ERROR", "保存数据失败")
		return
	}
	if !found {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "问题不存在")
		return
	}
	writeJSON(w, http.StatusOK, updated)
}

// 确保 fmt 包可用（未来调试用）
var _ = fmt.Sprintf
