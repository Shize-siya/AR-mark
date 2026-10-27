# AR 巡检系统 - Go 后端

## 环境

- Go 1.23+（零第三方依赖，仅用标准库）

## 启动

```bash
go run .
# 或编译后运行
go build -o server.exe .
./server.exe
```

服务监听 `:8080`，数据保存在 `data/issues.json`，重启后数据仍然存在。

## API 说明

| 方法 | 路径 | 功能 | 请求体 |
|------|------|------|--------|
| GET | `/health` | 服务状态检查 | - |
| POST | `/api/issues` | 创建问题（Unity 调用） | `{title*, description, priority*, position{x,y,z}}` |
| GET | `/api/issues` | 问题列表（React 调用），支持 `?status=` `?priority=` 过滤 | - |
| GET | `/api/issues/{id}` | 单个问题详情 | - |
| PATCH | `/api/issues/{id}/status` | 修改处理状态（React 调用） | `{status*}` |

- `priority` 枚举：`low` / `medium` / `high`
- `status` 枚举：`open`（新建默认）/ `in_progress` / `resolved`
- `id`、`createdAt`、`updatedAt` 由后端生成，时间为 UTC ISO 8601 格式
- 参数错误返回 `400 {"error":"INVALID_PARAMS","message":"..."}`；问题不存在返回 `404`
- 已配置 CORS 允许跨域，React 浏览器端可直接访问

## Unity 真机配置后端地址

1. 确保手机和电脑连接同一个 Wi-Fi
2. 电脑上运行 `ipconfig` 查看局域网 IP（如 `192.168.1.100`）
3. Unity 中将后端地址设置为 `http://192.168.1.100:8080`
4. 防火墙若拦截 8080 端口，需在 Windows 防火墙中放行

## 已知问题

- 无（基础功能均已实现并测试通过）
