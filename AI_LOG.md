# AI 使用记录

> 按实际顺序记录本题中所有主动发给 AI 的问题或指令。不含 IDE 自动补全。

## P-001
- 目标：读取飞书任务书，了解题目要求
### Prompt 原文
> 读取一下这个网页（飞书 wiki 链接），然后告诉我做这个程序的详细步骤，帮我把需要用到的工具，技术栈，软件也一并告诉我
### 结果
- 采用
- 验证方式：AI 通过浏览器输入密码后完整提取了文档原文，与任务书一致

## P-002
- 目标：实现 Go 后端
### Prompt 原文
> 我是安卓机，帮我先做一下go后端代码
### 结果
- 修改后采用
- AI 初版选择了纯标准库 + JSON 文件存储（而非 Gin + SQLite），接受该简化方案；启动服务后用 Postman/脚本逐项验证了 8 个接口场景（健康检查、创建、空标题 400、错误枚举 400、列表、改状态、单查、404）及重启持久化，全部通过

## P-003
- 目标：实现 React 管理端与 Unity AR 端
### Prompt 原文
> 接下来把剩下的第二阶段第三阶段做完
### 结果
- 采用
- 验证方式：React 端 `npm run build` 编译通过，并通过 Vite 代理与 Go 后端联调成功（列表、PATCH 改状态、页面 200）；Unity 部分因无法在命令行验证，按生成脚本报名的组件/API（ARRaycastManager、ARPlaneManager、UnityWebRequest）对照 AR Foundation 文档人工核对，待真机验证

## P-004
- 目标：自动化搭建 Unity AR 场景，避免手工连线出错
### Prompt 原文
> 接入一下unity的mcp帮我搭建一下unityar
### 结果
- 修改后采用
- 当前环境无 Unity MCP 可用（需自行安装桥接包与 MCP 服务），改用等效方案：让 AI 生成 Unity 编辑器自动化脚本 SceneBuilder.cs（DefaultControls 生成 UI + 脚本连线 + 安卓构建设置一键完成）；在 Unity 中执行菜单命令后通过 Console 无报错、场景对象与连线完整来验证

> 注：以上为开发过程中的实际记录。提交前请补充你自己与 AI 的其他对话（如有），并确认不含任何 API Key / Token 等敏感信息。

## P-005
- 目标：下载并配置 UnityMCP，实现 AI 直接控制 Unity 编辑器
### Prompt 原文
> 你去帮我下载GitHub上的UnityMCP然后帮我配置到我的电脑
### 结果
- 采用
- 验证方式：从 GitHub 下载 CoplayDev/unity-mcp v10.3.0；服务端用 `uv run src/main.py --help` 冒烟测试通过；插件包以嵌入式包放入项目 Packages/ 目录；Trae MCP 配置需用户手动粘贴 JSON 后重启验证
