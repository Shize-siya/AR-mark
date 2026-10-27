# 知识条目：技术栈总览

**条目ID：** ENTRY-002  
**分类：** 技术栈  
**关键词：** Unity, Go, React, AR Foundation, TypeScript, 技术选型  
**重要程度：** 高

---

## 固定技术栈

| 部分 | 技术要求 | 作用 |
|------|----------|------|
| 移动端 | Unity LTS、C#、AR Foundation | 检测平面、放置标记、上报问题 |
| 管理端 | React、TypeScript | 查看问题、修改状态 |
| 后端 | Go | 提供 API 并持久化数据 |

## 详细技术清单

### 移动端
- **游戏引擎：** Unity LTS（推荐 2022.3 LTS 或 6 LTS）
- **编程语言：** C#
- **AR 框架：** AR Foundation
- **AR 平台二选一：**
  - Android + ARCore
  - iOS + ARKit

### 后端
- **编程语言：** Go（1.21+）
- **Web 框架：** 不限（推荐 Gin、Echo、Fiber）
- **数据存储：** SQLite 或本地 JSON 文件

### 管理端
- **前端框架：** React 18+
- **语言：** TypeScript
- **构建工具：** Vite（推荐）或 Create React App
- **UI 组件库（可选）：** Ant Design、MUI

## 补充说明
- Unity 可选择 Android + ARCore 或 iOS + ARKit
- Go 框架不限
- 数据可保存到 SQLite 或本地 JSON 文件
- 可以使用 AI 辅助开发，但不要求产品本身接入大模型

## 来源
飞书文档《Unity-AR研发实操题-候选人任务书-简略版》第 2 节
