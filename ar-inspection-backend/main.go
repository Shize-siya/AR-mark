package main

import (
	"log"
	"net/http"
	"strings"
)

func corsMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PATCH, PUT, DELETE, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
		w.Header().Set("Access-Control-Expose-Headers", "Content-Type")

		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func main() {
	store, err := NewStore("data/issues.json")
	if err != nil {
		log.Fatalf("初始化数据存储失败: %v", err)
	}

	h := NewHandlers(store)
	mux := http.NewServeMux()

	// 健康检查
	mux.HandleFunc("GET /health", h.Health)

	// ===== 认证接口（无需 token）=====
	mux.HandleFunc("POST /api/auth/register", h.Register)
	mux.HandleFunc("POST /api/auth/login", h.Login)

	// ===== Issue 接口（公开可读；创建可带 token 关联用户）=====
	mux.HandleFunc("POST /api/issues", h.CreateIssue)
	mux.HandleFunc("GET /api/issues", h.ListIssues)
	mux.HandleFunc("GET /api/issues/{id}", func(w http.ResponseWriter, r *http.Request) {
		h.GetIssue(w, r, r.PathValue("id"))
	})
	mux.HandleFunc("PATCH /api/issues/{id}/status", func(w http.ResponseWriter, r *http.Request) {
		h.UpdateIssueStatus(w, r, r.PathValue("id"))
	})

	// ===== SSE 实时推送（公开可读）=====
	mux.HandleFunc("GET /api/events", h.Events)

	// ===== 当前用户（需要 token）=====
	mux.HandleFunc("GET /api/auth/me", authMiddleware(h.Me))

	// ===== 用户管理（需要 admin 权限）=====
	mux.HandleFunc("GET /api/users", authMiddleware(requireRole(h.ListUsers, RoleAdmin)))
	mux.HandleFunc("POST /api/users", authMiddleware(requireRole(h.CreateUser, RoleAdmin)))
	mux.HandleFunc("PATCH /api/users/{id}/role", authMiddleware(requireRole(func(w http.ResponseWriter, r *http.Request) {
		h.UpdateUserRole(w, r, r.PathValue("id"))
	}, RoleAdmin)))
	mux.HandleFunc("DELETE /api/users/{id}", authMiddleware(requireRole(func(w http.ResponseWriter, r *http.Request) {
		h.DeleteUser(w, r, r.PathValue("id"))
	}, RoleAdmin)))

	// 404
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if strings.HasPrefix(r.URL.Path, "/api/") || r.URL.Path == "/health" {
			writeError(w, http.StatusNotFound, "NOT_FOUND", "接口不存在: "+r.Method+" "+r.URL.Path)
			return
		}
		writeError(w, http.StatusNotFound, "NOT_FOUND", "not found")
	})

	addr := ":8080"
	log.Printf("========================================")
	log.Printf("AR 巡检后端已启动，监听 %s", addr)
	log.Printf("  健康检查: http://localhost:8080/health")
	log.Printf("  登录:     POST http://localhost:8080/api/auth/login")
	log.Printf("  默认账号: admin / admin123")
	log.Printf("  SSE:      GET  http://localhost:8080/api/events")
	log.Printf("========================================")
	log.Fatal(http.ListenAndServe(addr, corsMiddleware(mux)))
}
