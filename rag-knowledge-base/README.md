# RAG 知识库索引

**知识库名称：** Unity AR 园区巡检系统研发知识库  
**创建时间：** 2026-10-26  
**知识条目数量：** 8  
**文档数量：** 1  
**对话记录：** 1  

---

## 知识库目录结构

```
rag-knowledge-base/
├── README.md                    # 本文件（知识库总览）
├── documents/                   # 原始文档
│   └── 01-task-brief.md         # 飞书任务书原文
├── entries/                     # 结构化知识条目
│   ├── ENTRY-001-project-overview.md
│   ├── ENTRY-002-tech-stack.md
│   ├── ENTRY-003-data-structure.md
│   ├── ENTRY-004-unity-ar-mobile.md
│   ├── ENTRY-005-go-backend.md
│   ├── ENTRY-006-react-web.md
│   ├── ENTRY-007-delivery-requirements.md
│   └── ENTRY-008-dev-tools-and-workflow.md
└── conversation/                # 对话记录
    └── conversation-log.md
```

---

## 知识条目索引

| 条目ID | 标题 | 分类 | 关键词 | 重要程度 |
|--------|------|------|--------|----------|
| ENTRY-001 | 项目概述 | 项目概述 | 项目背景, AR巡检, 园区, 标记系统 | 高 |
| ENTRY-002 | 技术栈总览 | 技术栈 | Unity, Go, React, AR Foundation | 高 |
| ENTRY-003 | 基础数据结构 | 数据结构 | 数据模型, JSON, 字段定义, 枚举值 | 高 |
| ENTRY-004 | Unity AR 移动端功能 | 移动端 / Unity AR | 平面检测, 标记放置, ARCore, ARKit | 高 |
| ENTRY-005 | Go 后端功能 | 后端 / Go | REST API, SQLite, 数据持久化, CORS | 高 |
| ENTRY-006 | React 管理端功能 | 前端 / React | 问题列表, 状态管理, TypeScript | 高 |
| ENTRY-007 | 交付内容与要求 | 交付要求 | README, AI_LOG, 演示视频 | 高 |
| ENTRY-008 | 开发工具与环境 | 开发工具 | 工具, 软件, 环境配置, 开发流程 | 中 |

---

## 分类索引

### 项目概述类
- ENTRY-001 项目概述

### 技术栈类
- ENTRY-002 技术栈总览
- ENTRY-008 开发工具与环境

### 数据结构类
- ENTRY-003 基础数据结构

### 各端功能类
- ENTRY-004 Unity AR 移动端功能
- ENTRY-005 Go 后端功能
- ENTRY-006 React 管理端功能

### 交付类
- ENTRY-007 交付内容与要求

---

## 关键词索引

### 高频关键词
- **Unity AR**：ENTRY-001, ENTRY-002, ENTRY-004, ENTRY-008
- **Go 后端**：ENTRY-001, ENTRY-002, ENTRY-005, ENTRY-008
- **React 管理端**：ENTRY-001, ENTRY-002, ENTRY-006, ENTRY-008
- **AR Foundation**：ENTRY-002, ENTRY-004
- **数据结构**：ENTRY-003, ENTRY-004, ENTRY-005, ENTRY-006
- **平面检测**：ENTRY-004
- **REST API**：ENTRY-005
- **问题列表**：ENTRY-006
- **AI_LOG**：ENTRY-007
- **演示视频**：ENTRY-007

---

## 使用说明

### 检索建议
1. **快速概览**：先阅读 ENTRY-001 项目概述
2. **技术选型**：查阅 ENTRY-002 技术栈总览
3. **开发某一端**：直接查阅对应端的功能条目
4. **数据定义**：查阅 ENTRY-003 基础数据结构
5. **交付准备**：查阅 ENTRY-007 交付内容与要求
6. **开发流程**：查阅 ENTRY-008 开发工具与环境

### 原始文档
完整的任务书原文保存在 `documents/01-task-brief.md` 中。

### 对话历史
完整的对话记录保存在 `conversation/conversation-log.md` 中。

---

## 更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2026-10-26 | 初始版本，包含 8 个知识条目、1 份原始文档、1 份对话记录 |
