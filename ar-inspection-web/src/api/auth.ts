import type { LoginRequest, RegisterRequest, LoginResponse, UserPublic } from '../types/auth';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

function getToken(): string | null {
  return localStorage.getItem('auth_token');
}

function buildHeaders(extra: Record<string, string> = {}): Record<string, string> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json', ...extra };
  const token = getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return headers;
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: buildHeaders(),
    ...options,
  });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    if (res.status === 401) localStorage.removeItem('auth_token');
    throw new Error(body?.message ?? `请求失败: HTTP ${res.status}`);
  }
  // 204 No Content / 空响应体 → 返回 undefined
  if (res.status === 204 || res.contentLength === 0) {
    return undefined as T;
  }
  // 某些情况下 contentLength 未知但 body 仍为空 → 安全解析
  const text = await res.text();
  if (!text.trim()) return undefined as T;
  return JSON.parse(text) as T;
}

// ===== 认证 =====

export function login(data: LoginRequest): Promise<LoginResponse> {
  return request<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function register(data: RegisterRequest): Promise<UserPublic> {
  return request<UserPublic>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function getMe(): Promise<UserPublic> {
  return request<UserPublic>('/api/auth/me');
}

// ===== Issue =====

import type { Issue, IssueStatus, Priority } from '../types/issue';

export function getIssues(status?: IssueStatus | '', priority?: Priority | ''): Promise<Issue[]> {
  const params = new URLSearchParams();
  if (status) params.set('status', status);
  if (priority) params.set('priority', priority);
  const query = params.toString();
  return request<Issue[]>(`/api/issues${query ? `?${query}` : ''}`);
}

export function updateIssueStatus(id: string, status: IssueStatus): Promise<Issue> {
  return request<Issue>(`/api/issues/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  });
}

// ===== 用户管理（需要 admin）=====

export function listUsers(): Promise<UserPublic[]> {
  return request<UserPublic[]>('/api/users');
}

export function createUser(data: RegisterRequest): Promise<UserPublic> {
  return request<UserPublic>('/api/users', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function updateUserRole(id: string, role: string): Promise<UserPublic> {
  return request<UserPublic>(`/api/users/${id}/role`, {
    method: 'PATCH',
    body: JSON.stringify({ role }),
  });
}

export function deleteUser(id: string): Promise<void> {
  return request<void>(`/api/users/${id}`, { method: 'DELETE' });
}

// ===== Token 存取工具 =====

export function saveToken(token: string) {
  localStorage.setItem('auth_token', token);
}

export function clearToken() {
  localStorage.removeItem('auth_token');
}

export function hasToken(): boolean {
  return !!getToken();
}

export function apiBase(): string {
  return BASE_URL;
}
