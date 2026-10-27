import { useEffect, useState } from 'react';
import { listUsers, createUser, updateUserRole, deleteUser } from '../api/auth';
import type { UserPublic, Role } from '../types/auth';
import { ROLE_LABELS } from '../types/auth';

interface Props {
  currentUser: UserPublic;
  onLogout: () => void;
}

const ROLES: Role[] = ['admin', 'inspector', 'viewer'];

export default function UserManagement({ currentUser, onLogout }: Props) {
  const [users, setUsers] = useState<UserPublic[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newUser, setNewUser] = useState({ username: '', password: '', role: 'inspector' as Role });
  const [actionError, setActionError] = useState('');
  const [savingId, setSavingId] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await listUsers();
      setUsers(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : '加载用户列表失败');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionError('');
    if (!newUser.username || !newUser.password) {
      setActionError('用户名和密码不能为空');
      return;
    }
    try {
      await createUser(newUser);
      setNewUser({ username: '', password: '', role: 'inspector' });
      load();
    } catch (e) {
      setActionError(e instanceof Error ? e.message : '创建失败');
    }
  };

  const handleRoleChange = async (id: string, role: Role) => {
    setSavingId(id);
    setActionError('');
    try {
      await updateUserRole(id, role);
      load();
    } catch (e) {
      setActionError(e instanceof Error ? e.message : '修改失败');
    } finally {
      setSavingId(null);
    }
  };

  const handleDelete = async (id: string, username: string) => {
    if (!confirm(`确定要删除用户 "${username}" 吗？`)) return;
    setActionError('');
    try {
      await deleteUser(id);
      load();
    } catch (e) {
      setActionError(e instanceof Error ? e.message : '删除失败');
    }
  };

  return (
    <div className="user-mgmt">
      <header className="user-mgmt__header">
        <h2>人员管理</h2>
        <div className="user-mgmt__me">
          <span className="role-badge role-badge--admin">{ROLE_LABELS[currentUser.role]}</span>
          <span className="me-name">{currentUser.username}</span>
          <button className="btn-logout" onClick={onLogout}>登出</button>
        </div>
      </header>

      {actionError && <div className="error-banner">{actionError}</div>}

      <section className="create-user">
        <h3>新增用户</h3>
        <form className="create-user__form" onSubmit={handleCreate}>
          <input
            type="text"
            placeholder="用户名"
            value={newUser.username}
            onChange={(e) => setNewUser({ ...newUser, username: e.target.value })}
          />
          <input
            type="password"
            placeholder="密码（至少6位）"
            value={newUser.password}
            onChange={(e) => setNewUser({ ...newUser, password: e.target.value })}
          />
          <select
            value={newUser.role}
            onChange={(e) => setNewUser({ ...newUser, role: e.target.value as Role })}
          >
            {ROLES.map((r) => (
              <option key={r} value={r}>{ROLE_LABELS[r]}</option>
            ))}
          </select>
          <button type="submit">创建</button>
        </form>
      </section>

      <section className="user-table-wrap">
        <h3>用户列表 {loading && <span className="hint">加载中...</span>}</h3>
        <table className="user-table">
          <thead>
            <tr>
              <th>用户名</th>
              <th>角色</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>
                  {u.username}
                  {u.id === currentUser.id && <span className="you-tag">（当前）</span>}
                </td>
                <td>
                  <select
                    value={u.role}
                    onChange={(e) => handleRoleChange(u.id, e.target.value as Role)}
                    disabled={u.id === currentUser.id || savingId === u.id}
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>{ROLE_LABELS[r]}</option>
                    ))}
                  </select>
                </td>
                <td>{new Date(u.createdAt).toLocaleString('zh-CN', { hour12: false })}</td>
                <td>
                  <button
                    className="btn-danger"
                    onClick={() => handleDelete(u.id, u.username)}
                    disabled={u.id === currentUser.id}
                  >
                    删除
                  </button>
                </td>
              </tr>
            ))}
            {users.length === 0 && !loading && (
              <tr><td colSpan={4} className="empty-row">暂无用户</td></tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}
