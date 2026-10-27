# 知识条目：React 管理端功能

**条目ID：** ENTRY-006  
**分类：** 前端 / React  
**关键词：** React, TypeScript, Vite, 问题列表, 状态管理  
**重要程度：** 高

---

## 功能概述
React 管理端运行在电脑浏览器中，供管理人员查看巡检上报的问题列表，并可以修改问题的处理状态。

## 必做功能
- 展示问题列表
- 修改问题处理状态
- 可正常访问后端接口

## 核心功能详解

### 1. 问题列表页
- 以卡片或表格形式展示所有问题
- 显示问题标题（加粗）
- 显示问题描述（过长可截断）
- 优先级标签：不同优先级显示不同颜色
- 状态标签：不同状态显示不同颜色
- 坐标信息：显示 AR 标记位置坐标（x, y, z）
- 创建时间：格式化显示上报时间
- 状态修改操作：每条记录旁有状态修改入口

### 2. 状态标签样式
- **open（待处理）：** 红色标签
- **in_progress（处理中）：** 橙色/黄色标签
- **resolved（已解决）：** 绿色标签

### 3. 优先级标签样式
- **low（低）：** 灰色/蓝色标签
- **medium（中）：** 黄色/蓝色标签
- **high（高）：** 红色标签

### 4. 状态修改交互
- 点击状态下拉菜单，选择新状态
- 发送 PATCH 请求到后端更新状态
- 更新成功后刷新列表或更新本地状态
- 更新失败时显示错误提示

### 5. 筛选与刷新
- 状态筛选：下拉选择查看全部/待处理/处理中/已解决
- 优先级筛选：按优先级过滤
- 刷新按钮：手动重新获取最新数据
- 自动刷新（可选）：定时轮询获取最新数据

## TypeScript 类型定义
```typescript
interface Position {
  x: number;
  y: number;
  z: number;
}

type Priority = 'low' | 'medium' | 'high';
type IssueStatus = 'open' | 'in_progress' | 'resolved';

interface Issue {
  id: string;
  title: string;
  description: string;
  priority: Priority;
  status: IssueStatus;
  position: Position;
  createdAt: string;
  updatedAt: string;
}
```

## 项目结构推荐
```
ar-inspection-web/
├── src/
│   ├── api/              # API 请求封装
│   ├── components/       # 可复用组件
│   ├── types/            # TypeScript 类型定义
│   ├── App.tsx           # 主应用组件
│   ├── main.tsx          # 入口文件
│   └── index.css         # 全局样式
├── package.json
├── vite.config.ts
└── tsconfig.json
```

## 后端地址配置
- 开发环境：使用 Vite 代理或直接配置 http://localhost:8080
- 生产环境：通过环境变量 VITE_API_BASE_URL 配置
- 统一封装 axios 实例

## 来源
飞书文档《Unity-AR研发实操题-候选人任务书-简略版》第 3.2 节
