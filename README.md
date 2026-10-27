# Unity AR 园区巡检标记系统

园区巡检人员用手机 AR 在现场放置标记并上报问题，管理人员在网页端查看并更新处理状态。

```
Unity AR 放置标记并上报 → Go 后端保存 → React 页面查看问题 → React 修改处理状态
```

## 版本

| 部分 | 技术 | 版本 |
|------|------|------|
| 移动端 | Unity LTS + AR Foundation + ARCore | Unity 2022.3 LTS / 6 LTS |
| 后端 | Go（纯标准库，零第三方依赖） | Go 1.23.4 |
| 管理端 | React + TypeScript + Vite | React 19 / Node 24 |

## 目录结构

```
├── ar-inspection-backend/   # Go 后端（详见其 README）
├── ar-inspection-web/       # React 管理端（详见其 README）
└── ar-inspection-unity/     # Unity AR 移动端脚本与搭建指南（详见其 README）
```

## 快速启动

```bash
# 1. 后端（:8080）
cd ar-inspection-backend && go run .

# 2. 管理端（:5173）
cd ar-inspection-web && npm install && npm run dev

# 3. Unity：按 ar-inspection-unity/README.md 搭建场景并 Build And Run 到安卓真机
```

## 已完成

- Go 后端全部 9 项要求（健康检查、创建/查询/改状态、UUID、默认 open、参数校验、JSON 持久化、CORS）
- React 管理端（列表、状态修改、筛选、刷新、统计）
- Unity AR 脚本（平面检测、标记放置、表单上报）与完整搭建指南

## 未完成 / 已知问题

- Unity 场景与 UI 需按指南手动搭建（Unity 场景文件无法通过代码生成）
- 重启 App 后历史标记不恢复物理位置（题目明确不要求）

## 演示视频

（待录制：手机端 + 电脑端各 3~5 分钟）
