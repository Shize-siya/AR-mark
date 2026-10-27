# AR 巡检系统 - React 管理端

## 环境

- Node.js 18+

## 启动

```bash
npm install
npm run dev
```

打开 http://localhost:5173 ，开发服务器已配置代理，将 `/api`、`/health` 转发到 `http://localhost:8080` 的 Go 后端（需先启动后端）。

## 功能

- 问题列表：标题、描述、优先级、状态、AR 标记坐标、上报时间
- 状态修改：下拉切换 待处理(open) / 处理中(in_progress) / 已解决(resolved)
- 筛选：按状态、按优先级
- 刷新：手动获取最新数据
- 统计：各状态问题数量

## 后端地址配置

- 开发环境：使用 Vite 代理（见 `vite.config.ts`），无需配置
- 其他环境：设置环境变量 `VITE_API_BASE_URL=http://<后端地址>:8080`

## 已完成 / 已知问题

- 已完成：列表展示、状态修改、筛选、刷新、统计
- 已知问题：无自动轮询，需手动刷新
