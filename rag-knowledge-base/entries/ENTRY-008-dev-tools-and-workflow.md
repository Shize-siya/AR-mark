# 知识条目：开发工具与环境

**条目ID：** ENTRY-008  
**分类：** 开发工具  
**关键词：** 工具, 软件, 环境配置, Unity Hub, VS Code, Node.js  
**重要程度：** 中

---

## 必需软件清单

| 工具 | 用途 | 推荐版本 |
|------|------|----------|
| Unity Hub | Unity 版本管理与项目创建 | 最新版 |
| Unity Editor LTS | AR 应用开发 | 2022.3 LTS 或 6 LTS |
| Visual Studio / Rider | C# 代码编写 | 最新稳定版 |
| VS Code | Go + React 开发 | 最新版 |
| Go SDK | Go 语言开发环境 | 1.21+ |
| Node.js | React 项目构建 | 18 LTS 或 20 LTS |
| Git | 版本控制 | 最新版 |
| Postman / Apifox | 接口测试 | 最新版 |

## Android 开发额外工具
- **Android SDK / Android Studio**：Unity Android 构建支持
- **ARCore 支持的安卓真机**：AR 功能测试
- **USB 数据线**：真机调试与安装

## iOS 开发额外工具
- **Mac 电脑**：iOS 构建必需
- **Xcode**：iOS 应用构建与签名
- **支持 ARKit 的 iPhone/iPad**：AR 功能测试（A9 芯片及以上）
- **Apple 开发者账号**：应用签名与真机部署

## VS Code 推荐插件
- Go
- ESLint
- Prettier
- GitLens
- Thunder Client
- SQLite Viewer

## 推荐开发顺序
按照「后端 → 前端 → AR端」的顺序开发，每一步都有明确的验证目标。

### 第一阶段：Go 后端开发（1~2 天）
初始化项目 → 定义数据模型 → 实现数据库层 → 实现 API 接口 → 配置 CORS → 测试所有接口 → 验证数据持久化

### 第二阶段：React 管理端开发（1 天）
创建 Vite 项目 → 定义 TypeScript 类型 → 封装 API 请求 → 开发问题列表组件 → 开发状态修改功能 → 联调后端接口

### 第三阶段：Unity AR 移动端开发（2~3 天）
创建 Unity 项目 → 导入 AR Foundation → 配置平面检测 → 实现标记放置 → 制作 UI 表单 → 实现 HTTP 上报 → 真机部署测试

### 第四阶段：整体联调与交付（1 天）
端到端完整测试 → 编写 README.md → 整理 AI_LOG.md → 录制演示视频

## 开发建议
- 题目明确说"不要求复杂架构和精美界面"，优先保证功能跑通
- 后端可以先从简单的 JSON 文件存储开始，再考虑升级到 SQLite
- React 端可以先不用 UI 库，用原生 HTML + CSS 快速实现
- 三端字段名严格保持一致
- CORS 配置必须做好，否则 React 无法访问后端

## 来源
对话总结 + 飞书文档内容推导
