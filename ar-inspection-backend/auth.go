package main

import (
	"crypto/hmac"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/binary"
	"encoding/json"
	"errors"
	"net/http"
	"strings"
	"time"
)

// ===== 密码哈希：SHA256 + 随机 salt + 10000 轮迭代 =====

const saltLen = 16
const hashIterations = 10000

func hashPassword(password string) (string, error) {
	salt := make([]byte, saltLen)
	if _, err := rand.Read(salt); err != nil {
		return "", err
	}
	digest := sha256.Sum256([]byte(password + string(salt)))
	for i := 1; i < hashIterations; i++ {
		digest = sha256.Sum256(append(digest[:], password...))
	}
	// 格式: base64(salt):base64(hash)
	saltB64 := base64.StdEncoding.EncodeToString(salt)
	hashB64 := base64.StdEncoding.EncodeToString(digest[:])
	return saltB64 + ":" + hashB64, nil
}

func verifyPassword(password, storedHash string) bool {
	parts := strings.SplitN(storedHash, ":", 2)
	if len(parts) != 2 {
		return false
	}
	salt, err := base64.StdEncoding.DecodeString(parts[0])
	if err != nil {
		return false
	}
	stored, err := base64.StdEncoding.DecodeString(parts[1])
	if err != nil {
		return false
	}
	digest := sha256.Sum256([]byte(password + string(salt)))
	for i := 1; i < hashIterations; i++ {
		digest = sha256.Sum256(append(digest[:], password...))
	}
	return hmac.Equal(digest[:], stored)
}

// ===== JWT (HS256) =====

// JWT_SECRET 每次服务启动随机生成，重启后所有旧 token 失效
// 如需持久化登录，可改为从环境变量读取
var jwtSecret []byte

func init() {
	jwtSecret = make([]byte, 32)
	rand.Read(jwtSecret)
}

type JWTHeader struct {
	Alg string `json:"alg"`
	Typ string `json:"typ"`
}

type JWTClaims struct {
	Sub      string `json:"sub"`      // user ID
	Username string `json:"username"` // username
	Role     Role   `json:"role"`
	Iat      int64  `json:"iat"` // issued at
	Exp      int64  `json:"exp"` // expires at
}

func signToken(userID, username string, role Role) (string, error) {
	now := time.Now().UTC()
	claims := JWTClaims{
		Sub:      userID,
		Username: username,
		Role:     role,
		Iat:      now.Unix(),
		Exp:      now.Add(24 * time.Hour).Unix(), // 24 小时有效
	}
	header := JWTHeader{Alg: "HS256", Typ: "JWT"}

	headerJSON, _ := json.Marshal(header)
	claimsJSON, _ := json.Marshal(claims)
	headerB64 := base64.RawURLEncoding.EncodeToString(headerJSON)
	claimsB64 := base64.RawURLEncoding.EncodeToString(claimsJSON)

	signingInput := headerB64 + "." + claimsB64
	mac := hmac.New(sha256.New, jwtSecret)
	mac.Write([]byte(signingInput))
	sig := base64.RawURLEncoding.EncodeToString(mac.Sum(nil))

	return signingInput + "." + sig, nil
}

func verifyToken(token string) (JWTClaims, error) {
	parts := strings.Split(token, ".")
	if len(parts) != 3 {
		return JWTClaims{}, errors.New("token 格式错误")
	}

	signingInput := parts[0] + "." + parts[1]
	mac := hmac.New(sha256.New, jwtSecret)
	mac.Write([]byte(signingInput))
	expectedSig := base64.RawURLEncoding.EncodeToString(mac.Sum(nil))
	if !hmac.Equal([]byte(parts[2]), []byte(expectedSig)) {
		return JWTClaims{}, errors.New("token 签名无效")
	}

	claimsJSON, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return JWTClaims{}, errors.New("token claims 解码失败")
	}
	var claims JWTClaims
	if err := json.Unmarshal(claimsJSON, &claims); err != nil {
		return JWTClaims{}, errors.New("token claims 解析失败")
	}
	if time.Now().UTC().Unix() > claims.Exp {
		return JWTClaims{}, errors.New("token 已过期")
	}
	return claims, nil
}

// ===== Context 辅助：从请求提取用户 =====

type contextKey string

const userCtxKey contextKey = "auth_user"

type AuthUser struct {
	ID       string
	Username string
	Role     Role
}

// extractBearerToken 从 Authorization header 提取 Bearer token
func extractBearerToken(r *http.Request) string {
	auth := r.Header.Get("Authorization")
	if strings.HasPrefix(auth, "Bearer ") {
		return strings.TrimPrefix(auth, "Bearer ")
	}
	return ""
}

// ===== Auth handlers（注册、登录）=====

// ===== POST /api/auth/register =====

func (h *Handlers) Register(w http.ResponseWriter, r *http.Request) {
	var req RegisterRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if err := req.Validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", err.Error())
		return
	}

	// 检查用户名是否已存在
	if _, exists := h.store.GetUserByUsername(req.Username); exists {
		writeError(w, http.StatusConflict, "USER_EXISTS", "用户名已存在")
		return
	}

	hash, err := hashPassword(req.Password)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "HASH_ERROR", "密码加密失败")
		return
	}

	user := User{
		Username:     req.Username,
		PasswordHash: hash,
		Role:         req.Role,
		CreatedAt:    nowUTC(),
	}
	created, err := h.store.AddUser(user)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "STORE_ERROR", "保存用户失败")
		return
	}
	writeJSON(w, http.StatusCreated, created.Public())
}

// ===== POST /api/auth/login =====

func (h *Handlers) Login(w http.ResponseWriter, r *http.Request) {
	var req LoginRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if err := req.Validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", err.Error())
		return
	}

	user, found := h.store.GetUserByUsername(req.Username)
	if !found || !verifyPassword(req.Password, user.PasswordHash) {
		writeError(w, http.StatusUnauthorized, "INVALID_CREDENTIALS", "用户名或密码错误")
		return
	}

	token, err := signToken(user.ID, user.Username, user.Role)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "TOKEN_ERROR", "生成 token 失败")
		return
	}
	writeJSON(w, http.StatusOK, LoginResponse{Token: token, User: user.Public()})
}

// ===== GET /api/auth/me — 返回当前登录用户信息 =====

func (h *Handlers) Me(w http.ResponseWriter, r *http.Request) {
	au := r.Context().Value(userCtxKey).(*AuthUser)
	user, found := h.store.GetUserByID(au.ID)
	if !found {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "用户不存在")
		return
	}
	writeJSON(w, http.StatusOK, user.Public())
}

// ===== User management（需要 admin 权限）=====

// ===== GET /api/users —— 用户列表 =====

func (h *Handlers) ListUsers(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, h.store.ListUsers())
}

// ===== POST /api/users —— 管理员新增用户 =====

func (h *Handlers) CreateUser(w http.ResponseWriter, r *http.Request) {
	var req RegisterRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if err := req.Validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", err.Error())
		return
	}

	if _, exists := h.store.GetUserByUsername(req.Username); exists {
		writeError(w, http.StatusConflict, "USER_EXISTS", "用户名已存在")
		return
	}

	hash, err := hashPassword(req.Password)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "HASH_ERROR", "密码加密失败")
		return
	}

	user := User{
		Username:     req.Username,
		PasswordHash: hash,
		Role:         req.Role,
		CreatedAt:    nowUTC(),
	}
	created, err := h.store.AddUser(user)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "STORE_ERROR", "保存用户失败")
		return
	}
	writeJSON(w, http.StatusCreated, created.Public())
}

// ===== PATCH /api/users/{id}/role —— 修改用户角色 =====

func (h *Handlers) UpdateUserRole(w http.ResponseWriter, r *http.Request, id string) {
	var body struct {
		Role Role `json:"role"`
	}
	if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法的 JSON")
		return
	}
	if !body.Role.Valid() {
		writeError(w, http.StatusBadRequest, "INVALID_PARAMS", "role 错误，可选值: admin / inspector / viewer")
		return
	}

	au := r.Context().Value(userCtxKey).(*AuthUser)
	// 不能修改自己的角色
	if au.ID == id {
		writeError(w, http.StatusForbidden, "CANNOT_MODIFY_SELF", "不能修改自己的角色")
		return
	}

	updated, found, err := h.store.UpdateUserRole(id, body.Role)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "STORE_ERROR", "保存失败")
		return
	}
	if !found {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "用户不存在")
		return
	}
	writeJSON(w, http.StatusOK, updated.Public())
}

// ===== DELETE /api/users/{id} =====

func (h *Handlers) DeleteUser(w http.ResponseWriter, r *http.Request, id string) {
	au := r.Context().Value(userCtxKey).(*AuthUser)
	if au.ID == id {
		writeError(w, http.StatusForbidden, "CANNOT_DELETE_SELF", "不能删除自己")
		return
	}

	if !h.store.DeleteUser(id) {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "用户不存在")
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// 确保 unused 函数不会被编译器报 unused
var _ = binary.BigEndian
