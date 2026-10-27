import { useState } from 'react';
import type { Issue, IssueStatus } from '../types/issue';
import { STATUS_LABELS, PRIORITY_LABELS } from '../types/issue';
import { updateIssueStatus } from '../api/auth';

interface Props {
  issue: Issue;
  onUpdated: (issue: Issue) => void;
}

const STATUS_OPTIONS: IssueStatus[] = ['open', 'in_progress', 'resolved'];

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('zh-CN', { hour12: false });
}

export default function IssueCard({ issue, onUpdated }: Props) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const handleStatusChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newStatus = e.target.value as IssueStatus;
    if (newStatus === issue.status) return;

    setSaving(true);
    setError('');
    try {
      const updated = await updateIssueStatus(issue.id, newStatus);
      onUpdated(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : '状态更新失败');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={`issue-card issue-card--${issue.status}`}>
      <div className="issue-card__header">
        <h3 className="issue-card__title">{issue.title}</h3>
        <div className="issue-card__badges">
          <span className={`badge badge--priority-${issue.priority}`}>
            优先级: {PRIORITY_LABELS[issue.priority]}
          </span>
          <span className={`badge badge--status-${issue.status}`}>
            {STATUS_LABELS[issue.status]}
          </span>
        </div>
      </div>

      {issue.description && (
        <p className="issue-card__desc">{issue.description}</p>
      )}

      <div className="issue-card__meta">
        <span>
          标记位置: ({issue.position.x.toFixed(2)}, {issue.position.y.toFixed(2)},{' '}
          {issue.position.z.toFixed(2)})
        </span>
        <span>上报时间: {formatTime(issue.createdAt)}</span>
        {issue.createdBy && <span className="issue-card__author">上报人: {issue.createdBy}</span>}
      </div>

      <div className="issue-card__actions">
        <label>
          处理状态：
          <select
            value={issue.status}
            onChange={handleStatusChange}
            disabled={saving}
          >
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABELS[s]}
              </option>
            ))}
          </select>
        </label>
        {saving && <span className="issue-card__hint">保存中...</span>}
        {error && <span className="issue-card__error">{error}</span>}
      </div>
    </div>
  );
}
