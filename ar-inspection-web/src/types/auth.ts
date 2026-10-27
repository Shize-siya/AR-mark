export type Role = 'admin' | 'inspector' | 'viewer';

export interface UserPublic {
  id: string;
  username: string;
  role: Role;
  createdAt: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  role?: Role;
}

export interface LoginResponse {
  token: string;
  user: UserPublic;
}

export const ROLE_LABELS: Record<Role, string> = {
  admin: '管理员',
  inspector: '巡检员',
  viewer: '查看者',
};

export const ROLE_COLORS: Record<Role, string> = {
  admin: '#d94040',
  inspector: '#3b82f6',
  viewer: '#64748b',
};
