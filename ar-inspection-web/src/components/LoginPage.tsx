import { useState } from 'react';
import { login, register, saveToken } from '../api/auth';
import type { UserPublic, Role } from '../types/auth';
import { ROLE_LABELS } from '../types/auth';

interface Props {
  onAuthenticated: (user: UserPublic) => void;
}

export default function LoginPage({ onAuthenticated }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<Role>('inspector');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      if (mode === 'login') {
        const res = await login({ username, password });
        saveToken(res.token);
        onAuthenticated(res.user);
      } else {
        const user = await register({ username, password, role });
        // 注册后自动登录
        const res = await login({ username, password });
        saveToken(res.token);
        onAuthenticated(res.user);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '操作失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-wrap">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">🎯</div>
          <h1>AR 园区巡检</h1>
          <p className="login-subtitle">{mode === 'login' ? '登录管理后台' : '注册新账号'}</p>
        </div>

        <div className="login-tabs">
          <button
            className={`login-tab ${mode === 'login' ? 'active' : ''}`}
            onClick={() => setMode('login')}
            type="button"
          >
            登录
          </button>
          <button
            className={`login-tab ${mode === 'register' ? 'active' : ''}`}
            onClick={() => setMode('register')}
            type="button"
          >
            注册
          </button>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <label className="login-field">
            <span>用户名</span>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="请输入用户名"
              autoComplete="username"
              disabled={loading}
            />
          </label>

          <label className="login-field">
            <span>密码</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="请输入密码"
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              disabled={loading}
            />
          </label>

          {mode === 'register' && (
            <label className="login-field">
              <span>角色</span>
              <select value={role} onChange={(e) => setRole(e.target.value as Role)} disabled={loading}>
                {Object.entries(ROLE_LABELS).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </label>
          )}

          {error && <div className="login-error">⚠ {error}</div>}

          <button type="submit" className="login-submit" disabled={loading}>
            {loading ? '处理中...' : mode === 'login' ? '登 录' : '注 册'}
          </button>

          {mode === 'login' && (
            <div className="login-hint">
              默认管理员：<code>admin</code> / <code>admin123</code>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
