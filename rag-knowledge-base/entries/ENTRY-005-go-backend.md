# 知识条目：Go 后端功能

**条目ID：** ENTRY-005  
**分类：** 后端 / Go  
**关键词：** Go, Gin, REST API, SQLite, 数据持久化, CORS  
**重要程度：** 高

---

## 功能概述
Go 后端是整个系统的中枢，提供 RESTful API 接口，负责接收 Unity 上报的问题、持久化存储数据、向 React 管理端提供数据查询和状态修改服务。

## 必做功能
1. 提供简单的服务运行状态检查
2. 接收并保存 Unity 上报的问题
3. 返回已经保存的问题列表
4. 根据问题 ID 修改处理状态
5. 为新问题生成唯一 ID，默认状态为 `open`
6. 标题为空或枚举值错误时，返回明确的参数错误
7. 数据在 Go 服务重启后仍然存在
8. Unity 和 React 使用同一个 Go 后端和同一份数据
9. React 在浏览器中能够正常访问后端

## API 接口设计

### 1. 健康检查
- **方法：** GET
- **路径：** /health
- **功能：** 服务运行状态检查
- **响应：** status + timestamp

### 2. 创建问题
- **方法：** POST
- **路径：** /api/issues
- **功能：** 接收 Unity 提交的新问题
- **请求体：** title, description, priority, position{x,y,z}
- **响应：** 创建成功的完整问题对象

### 3. 获取问题列表
- **方法：** GET
- **路径：** /api/issues
- **功能：** 返回已保存的问题列表
- **可选参数：** status, priority 过滤
- **响应：** 问题对象数组

### 4. 获取单个问题
- **方法：** GET
- **路径：** /api/issues/:id
- **功能：** 获取单个问题详情
- **响应：** 单个问题对象，404 表示不存在

### 5. 修改问题状态
- **方法：** PATCH / PUT
- **路径：** /api/issues/:id/status
- **功能：** 修改问题处理状态
- **请求体：** status
- **响应：** 更新后的完整问题对象

## 业务逻辑规则
- **ID 生成：** 新问题由后端生成唯一 ID（推荐 UUID v4）
- **默认状态：** 新创建的问题 status 默认为 open
- **时间戳：** createdAt 和 updatedAt 由后端自动生成
- **标题校验：** title 为空时返回 400 参数错误
- **枚举校验：** priority 和 status 必须是预定义的枚举值
- **数据持久化：** 服务重启后数据仍然存在（SQLite 或 JSON 文件）
- **CORS 跨域：** 允许浏览器端（React）跨域访问 API
- **状态更新：** 更新状态时自动刷新 updatedAt

## 技术选型
- **Web 框架：** 不限（推荐 Gin、Echo、Fiber）
- **数据存储：** SQLite 或本地 JSON 文件
- **ORM（可选）：** GORM

## 项目结构推荐
```
ar-inspection-backend/
├── main.go           # 入口文件
├── go.mod / go.sum
├── handler/          # HTTP 处理器
├── model/            # 数据模型
├── repository/       # 数据访问层
├── service/          # 业务逻辑层
└── data/             # 数据文件目录
```

## 来源
飞书文档《Unity-AR研发实操题-候选人任务书-简略版》第 3.3 节
