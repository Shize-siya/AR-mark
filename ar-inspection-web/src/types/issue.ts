// 与 Go 后端保持一致的数据结构
export interface Position {
  x: number;
  y: number;
  z: number;
}

export type Priority = 'low' | 'medium' | 'high';
export type IssueStatus = 'open' | 'in_progress' | 'resolved';

export interface Issue {
  id: string;
  title: string;
  description: string;
  priority: Priority;
  status: IssueStatus;
  position: Position;
  createdBy?: string; // 用户名（可选，可能是匿名提交）
  createdAt: string;
  updatedAt: string;
}

export const STATUS_LABELS: Record<IssueStatus, string> = {
  open: '待处理',
  in_progress: '处理中',
  resolved: '已解决',
};

export const PRIORITY_LABELS: Record<Priority, string> = {
  low: '低',
  medium: '中',
  high: '高',
};
