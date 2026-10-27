package main

import (
	"crypto/rand"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sync"
)

// Store 是基于本地 JSON 文件的存储，服务重启后数据仍然存在
type Store struct {
	mu     sync.RWMutex
	path   string // issues.json 路径
	issues []Issue
	users  []User

	// SSE 广播器
	broker *Broker
}

// NewStore 加载（或创建）数据文件
func NewStore(issuesPath string) (*Store, error) {
	s := &Store{
		path:   issuesPath,
		users:  []User{},
		issues: []Issue{},
		broker: NewBroker(),
	}

	// 加载 issues
	data, err := os.ReadFile(issuesPath)
	if err != nil {
		if os.IsNotExist(err) {
			if err := s.saveIssues(); err != nil {
				return nil, err
			}
		} else {
			return nil, err
		}
	} else if len(data) > 0 {
		if err := json.Unmarshal(data, &s.issues); err != nil {
			return nil, fmt.Errorf("解析 issues.json 失败: %w", err)
		}
	}

	// 加载 users
	p := s.userPath()
	udata, err := os.ReadFile(p)
	if err != nil {
		if os.IsNotExist(err) {
			if err := s.saveUsers(); err != nil {
				return nil, err
			}
		} else {
			return nil, err
		}
	} else if len(udata) > 0 {
		if err := json.Unmarshal(udata, &s.users); err != nil {
			return nil, fmt.Errorf("解析 users.json 失败: %w", err)
		}
	}

	// 确保至少有一个 admin 用户
	s.ensureDefaultAdmin()

	return s, nil
}

func (s *Store) saveIssues() error {
	if err := os.MkdirAll(filepath.Dir(s.path), 0o755); err != nil {
		return err
	}
	data, err := json.MarshalIndent(s.issues, "", "  ")
	if err != nil {
		return err
	}
	tmp := s.path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return err
	}
	return os.Rename(tmp, s.path)
}

func (s *Store) userPath() string {
	dir := filepath.Dir(s.path)
	return filepath.Join(dir, "users.json")
}

func (s *Store) saveUsers() error {
	p := s.userPath()
	if err := os.MkdirAll(filepath.Dir(p), 0o755); err != nil {
		return err
	}
	data, err := json.MarshalIndent(s.users, "", "  ")
	if err != nil {
		return err
	}
	tmp := p + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return err
	}
	return os.Rename(tmp, p)
}

func newUUID() string {
	var b [16]byte
	rand.Read(b[:])
	b[6] = (b[6] & 0x0f) | 0x40
	b[8] = (b[8] & 0x3f) | 0x80
	return fmt.Sprintf("%x-%x-%x-%x-%x", b[0:4], b[4:6], b[6:8], b[8:10], b[10:16])
}

// ===== Issue CRUD =====

func (s *Store) AddIssue(issue Issue) (Issue, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	issue.ID = newUUID()
	s.issues = append(s.issues, issue)

	if err := s.saveIssues(); err != nil {
		return Issue{}, err
	}
	s.broker.Publish(SSEEvent{Type: "issue.created", Data: issue, Time: nowUTC()})
	return issue, nil
}

func (s *Store) ListIssues(status Status, priority Priority) []Issue {
	s.mu.RLock()
	defer s.mu.RUnlock()

	result := make([]Issue, 0, len(s.issues))
	for i := len(s.issues) - 1; i >= 0; i-- {
		issue := s.issues[i]
		if status != "" && issue.Status != status {
			continue
		}
		if priority != "" && issue.Priority != priority {
			continue
		}
		result = append(result, issue)
	}
	return result
}

func (s *Store) GetIssueByID(id string) (Issue, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	for _, issue := range s.issues {
		if issue.ID == id {
			return issue, true
		}
	}
	return Issue{}, false
}

func (s *Store) UpdateIssueStatus(id string, status Status) (Issue, bool, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	for i, issue := range s.issues {
		if issue.ID == id {
			s.issues[i].Status = status
			s.issues[i].UpdatedAt = nowUTC()
			if err := s.saveIssues(); err != nil {
				return Issue{}, false, err
			}
			s.broker.Publish(SSEEvent{Type: "issue.updated", Data: s.issues[i], Time: nowUTC()})
			return s.issues[i], true, nil
		}
	}
	return Issue{}, false, nil
}

// ===== User CRUD =====

func (s *Store) AddUser(user User) (User, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	user.ID = newUUID()
	s.users = append(s.users, user)

	if err := s.saveUsers(); err != nil {
		return User{}, err
	}
	s.broker.Publish(SSEEvent{Type: "user.created", Data: user.Public(), Time: nowUTC()})
	return user, nil
}

func (s *Store) GetUserByUsername(username string) (User, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	for _, u := range s.users {
		if u.Username == username {
			return u, true
		}
	}
	return User{}, false
}

func (s *Store) GetUserByID(id string) (User, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	for _, u := range s.users {
		if u.ID == id {
			return u, true
		}
	}
	return User{}, false
}

func (s *Store) ListUsers() []UserPublic {
	s.mu.RLock()
	defer s.mu.RUnlock()
	result := make([]UserPublic, len(s.users))
	for i, u := range s.users {
		result[i] = u.Public()
	}
	return result
}

func (s *Store) DeleteUser(id string) bool {
	s.mu.Lock()
	defer s.mu.Unlock()
	for i, u := range s.users {
		if u.ID == id {
			s.users = append(s.users[:i], s.users[i+1:]...)
			if err := s.saveUsers(); err != nil {
				return false
			}
			s.broker.Publish(SSEEvent{Type: "user.deleted", Data: id, Time: nowUTC()})
			return true
		}
	}
	return false
}

func (s *Store) UpdateUserRole(id string, role Role) (User, bool, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	for i, u := range s.users {
		if u.ID == id {
			s.users[i].Role = role
			if err := s.saveUsers(); err != nil {
				return User{}, false, err
			}
			return s.users[i], true, nil
		}
	}
	return User{}, false, nil
}

// ensureDefaultAdmin 如果没有任何用户，自动创建一个 admin
func (s *Store) ensureDefaultAdmin() {
	if len(s.users) > 0 {
		return
	}
	hash, _ := hashPassword("admin123")
	admin := User{
		ID:           newUUID(),
		Username:     "admin",
		PasswordHash: hash,
		Role:         RoleAdmin,
		CreatedAt:    nowUTC(),
	}
	s.users = append(s.users, admin)
	if err := s.saveUsers(); err != nil {
		fmt.Fprintf(os.Stderr, "创建默认 admin 用户失败: %v\n", err)
		return
	}
	fmt.Println("=== 首次启动：已创建默认管理员账号 ===")
	fmt.Println("  用户名: admin")
	fmt.Println("  密码:   admin123")
	fmt.Println("  请尽快登录后修改密码！")
	fmt.Println("=======================================")
}

func (s *Store) Broker() *Broker {
	return s.broker
}
