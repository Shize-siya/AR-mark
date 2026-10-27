import { useCallback, useEffect, useRef, useState } from 'react';
import type { Issue, IssueStatus, Priority } from './types/issue';
import { STATUS_LABELS, PRIORITY_LABELS } from './types/issue';
import type { UserPublic, Role } from './types/auth';
import { ROLE_LABELS } from './types/auth';
import { getIssues, getMe, clearToken, apiBase, hasToken } from './api/auth';
import IssueCard from './components/IssueCard';
import LoginPage from './components/LoginPage';
import UserManagement from './components/UserManagement';

type Tab = 'issues' | 'users';

interface SSEEvent {
  type: string;
  data: unknown;
  time: string;
}

export default function App() {
  // ===== 登录状态 =====
  const [user, setUser] = useState<UserPublic | null>(null);
  const [authLoading, setAuthLoading] = useState(true);
  const [currentTab, setCurrentTab] = useState<Tab>('issues');

  // ===== Issue 列表 =====
  const [issues, setIssues] = useState<Issue[]>([]);
  const [issuesLoading, setIssuesLoading] = useState(true);
  const [issuesError, setIssuesError] = useState('');
  const [statusFilter, setStatusFilter] = useState<IssueStatus | ''>('');
  const [priorityFilter, setPriorityFilter] = useState<Priority | ''>('');

  const sseRef = useRef<EventSource | null>(null);
  const reconnectTimerRef = useRef<number | null>(null);

  // ===== 初始化：恢复登录态 =====
  useEffect(() => {
    if (!hasToken()) {
      setAuthLoading(false);
      return;
    }
    getMe()
      .then((u) => setUser(u))
      .catch(() => clearToken())
      .finally(() => setAuthLoading(false));
  }, []);

  // ===== 登出 =====
  const handleLogout = () => {
    clearToken();
    setUser(null);
    setIssues([]);
    setCurrentTab('issues');
  };

  const handleAuthenticated = (u: UserPublic) => {
    setUser(u);
    setCurrentTab('issues');
  };

  // ===== 加载 Issue =====
  const loadIssues = useCallback(async () => {
    setIssuesLoading(true);
    setIssuesError('');
    try {
      const data = await getIssues(statusFilter, priorityFilter);
      setIssues(data);
    } catch (err) {
      setIssuesError(err instanceof Error ? err.message : '加载失败');
    } finally {
      setIssuesLoading(false);
    }
  }, [statusFilter, priorityFilter]);

  useEffect(() => {
    if (user) loadIssues();
  }, [user, loadIssues]);

  // ===== SSE 实时推送 =====
  const connectSSE = useCallback(() => {
    // 清掉重连定时器
    if (reconnectTimerRef.current !== null) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }
    // 旧连接
    if (sseRef.current) {
      try { sseRef.current.close(); } catch {}
    }
    const url = `${apiBase()}/api/events`;
    console.log('[SSE] connecting to', url);
    const es = new EventSource(url);
    sseRef.current = es;

    es.onopen = () => {
      console.log('[SSE] connected');
    };

    es.onmessage = (ev) => {
      const evt: SSEEvent = JSON.parse(ev.data);
      console.log('[SSE] event:', evt.type);
      if (evt.type === 'issue.created' || evt.type === 'issue.updated') {
        const issue = evt.data as Issue;
        setIssues((prev) => {
          const idx = prev.findIndex((i) => i.id === issue.id);
          if (idx >= 0) {
            const copy = [...prev];
            copy[idx] = issue;
            return copy;
          }
          // 新建问题插到最前
          return [issue, ...prev].slice(0, 200);
        });
      }
    };

    es.onerror = () => {
      console.warn('[SSE] error, reconnecting in 3s...');
      try { es.close(); } catch {}
      sseRef.current = null;
      reconnectTimerRef.current = window.setTimeout(connectSSE, 3000);
    };
  }, []);

  useEffect(() => {
    if (user) {
      connectSSE();
    }
    return () => {
      if (sseRef.current) {
        try { sseRef.current.close(); } catch {}
      }
      if (reconnectTimerRef.current !== null) {
        clearTimeout(reconnectTimerRef.current);
      }
    };
  }, [user, connectSSE]);

  const handleUpdated = (updated: Issue) => {
    setIssues((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
  };

  if (authLoading) {
    return <div className="app-init">正在恢复登录...</div>;
  }

  if (!user) {
    return <LoginPage onAuthenticated={handleAuthenticated} />;
  }

  // ===== 登录后主界面 =====
  const openCount = issues.filter((i) => i.status === 'open').length;
  const inProgressCount = issues.filter((i) => i.status === 'in_progress').length;
  const resolvedCount = issues.filter((i) => i.status === 'resolved').length;
  const isAdmin = user.role === 'admin';

  return (
    <div className="app">
      <header className="app__header">
        <div className="app__header-left">
          <h1>🎯 园区巡检问题管理</h1>
        </div>
        <div className="app__header-right">
          {isAdmin && (
            <nav className="app-tabs">
              <button
                className={`app-tab ${currentTab === 'issues' ? 'active' : ''}`}
                onClick={() => setCurrentTab('issues')}
              >
                问题列表
              </button>
              <button
                className={`app-tab ${currentTab === 'users' ? 'active' : ''}`}
                onClick={() => setCurrentTab('users')}
              >
                人员管理
              </button>
            </nav>
          )}
          <div className="app__me">
            <span className={`role-badge role-badge--${user.role}`}>{ROLE_LABELS[user.role]}</span>
            <span className="me-name">{user.username}</span>
            <button className="btn-logout" onClick={handleLogout}>登出</button>
          </div>
        </div>
      </header>

      {currentTab === 'users' && isAdmin ? (
        <UserManagement currentUser={user} onLogout={handleLogout} />
      ) : (
        <>
          <div className="toolbar">
            <div className="toolbar__filters">
              <label>
                状态：
                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as IssueStatus | '')}>
                  <option value="">全部</option>
                  {Object.entries(STATUS_LABELS).map(([v, l]) => (
                    <option key={v} value={v}>{l}</option>
                  ))}
                </select>
              </label>
              <label>
                优先级：
                <select value={priorityFilter} onChange={(e) => setPriorityFilter(e.target.value as Priority | '')}>
                  <option value="">全部</option>
                  {Object.entries(PRIORITY_LABELS).map(([v, l]) => (
                    <option key={v} value={v}>{l}</option>
                  ))}
                </select>
              </label>
            </div>
            <div className="toolbar__right">
              <span className="live-indicator" title="实时推送已连接">● 实时同步中</span>
              <button className="btn-refresh" onClick={loadIssues} disabled={issuesLoading}>
                {issuesLoading ? '加载中...' : '手动刷新'}
              </button>
            </div>
          </div>

          <div className="stats">
            <span className="stat stat--open">待处理 {openCount}</span>
            <span className="stat stat--progress">处理中 {inProgressCount}</span>
            <span className="stat stat--resolved">已解决 {resolvedCount}</span>
            <span className="stat">共 {issues.length} 条</span>
          </div>

          {issuesError && <div className="error-banner">{issuesError}</div>}

          <main className="issue-list">
            {!issuesLoading && issues.length === 0 && !issuesError && (
              <div className="empty-state">
                暂无问题。使用 Unity AR 应用在现场放置标记并上报后，问题会显示在这里（实时推送自动刷新）。
              </div>
            )}
            {issues.map((issue) => (
              <IssueCard key={issue.id} issue={issue} onUpdated={handleUpdated} />
            ))}
          </main>
        </>
      )}
    </div>
  );
}
