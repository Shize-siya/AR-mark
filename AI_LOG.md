# AI 交互日志

> 记录本题中所有由用户主动发给 AI 的问题与指令，按实际时间顺序排列。

---

## P-001

- 目标：AR Plane 贴合地面、Marker+表单弹出，但手机提交卡在"提交中"

### Prompt 原文

> （上一轮会话，用户初始搭建 AR 园区巡检系统后）连上同一个wifi了，一直显示提交中，帮我修复，同时看看后端和管理端打开了吗

### 结果

- **采用**
- 验证：PC curl localhost:8080 → 200 OK；手机 curl WiFi IP → 丢包但 TCP 可达；决定改用 `adb reverse tcp:8080 tcp:8080` + Unity URL 改 `http://localhost:8080`

---

## P-002

- 目标：底部表单大小比例问题，优先级选择框看不见

### Prompt 原文

> 现在可以提交上去了，然后修改一下底部表单的大小比例问题，优先级的选择框看不见了

### 结果

- **采用**
- 验证：FormPanel 高度 400→500px；所有子元素从中心锚定改为底部锚定 + pivot=(0.5,0)，AnchoredPosition.y = 距面板底部距离

---

## P-003

- 目标：黑色半透明背景只有一半

### Prompt 原文

> 后面的黑色半透明背景只有一半，选择优先级的选择框还是看不见

### 结果

- **采用**
- 验证：Canvas anchorMin=(0,0) anchorMax=(1,1) sizeDelta=(0,0) 撑满全屏；重建 APK 安装

---

## P-004

- 目标：App 看不见修改效果

### Prompt 原文

> app是不是没重新安装我看不见修改的地方

### 结果

- **采用**
- 根因：之前 execute_code 改的是内存值，没 SaveScene 到磁盘
- 验证：Unity 里改值 + `EditorSceneManager.SaveScene()`，grep 验证 YAML 变化后再构建

---

## P-005

- 目标：优先级选择框里没有字和选项

### Prompt 原文

> 选择优先级的里面还是没有字和选项，其他正常了

### 结果

- **采用**
- 根因：Dropdown 组件 template/captionText/itemText 三个引用全为 NULL
- 验证：用代码动态创建完整 Dropdown 内部层级（CaptionText + Viewport + Content + Item）

---

## P-006

- 目标：下拉列表不弹出 + 字体改黑色

### Prompt 原文

> 没有弹出下拉列表，然后把这个低中高改成黑色字体

### 结果

- **采用**
- Dropdown template 重建（ScrollRect+Mask+VerticalLayoutGroup），字体统一改黑色
- 验证：手机上点击下拉框能弹出列表、低/中/高黑色可见

---

## P-007

- 目标：下拉还是不弹，改不了优先级

### Prompt 原文

> 还是改不了优先级

### 结果

- **采用（方案变更）**
- 根因：Unity 原生 Dropdown 组件 template 太脆弱
- 验证：彻底砍掉 Dropdown，换成三个按钮（Btn_低绿/Btn_中黄/Btn_高红），直接 onClick → SetPriority(index)

---

## P-008

- 目标：按钮没字 + 选中高亮

### Prompt 原文

> 按钮上没有字了，我希望按钮按下去时可以看出来哪个按钮按下去了

### 结果

- **采用**
- 根因：创建 Label 时没给 txt.text 赋值
- 验证：SetPriority 里调用 UpdatePriorityButtonHighlight()，选中按钮变深色+scale 1.05，未选中 scale 0.97

---

## P-009

- 目标：登录、权限/人员管理、React 管理端实时更新

### Prompt 原文

> 接下来做 登录、权限或人员管理；然后让react管理端实时更新

### 结果

- **采用**
- 后端新增 JWT 认证 + 用户管理 9 个 API + SSE `/api/events` 广播器；React 加 LoginPage + UserManagement + EventSource 实时推送
- 验证：admin/admin123 登录 → 提交 issue → 管理端秒级刷新

---

## P-010

- 目标：删除用户时报 JSON 解析错误

### Prompt 原文

> 当删除人员时出现这个Failed to execute 'json' on 'Response': Unexpected end of JSON input报错

### 结果

- **采用**
- 根因：DELETE 返回 HTTP 204 No Content（空 body），前端 request() 无条件调 res.json()
- 验证：改 request() — 状态码 204 / contentLength 0 / 空 text 时直接返回 undefined

---

## P-011

- 目标：提交到 GitHub

### Prompt 原文

> 先把这些git到git@github.com:Shize-siya/AR-mark.git这个仓库，git commit的内容是基本功能已经实现

### 结果

- **采用**
- 验证：git init + 写 .gitignore（Unity Library/node_modules/exe 全排除）+ git add -A + commit + push

---

## P-012

- 目标：绿色平面改半透明 + Marker 按优先级/状态改样式

### Prompt 原文

> 现在把这个绿色面改成半透明然后实现根据优先级或状态改变 AR 标记样式；

### 结果

- **采用**
- PlaneMat：Standard → Transparent（_Mode=3, ZWrite=0, renderQueue=3000），颜色 RGBA(0.15,0.85,0.35,0.35)
- PlaceMarkerOnPlane 加 ApplyMarkerStyle(int)：低=绿+0.12x / 中=黄+0.16x / 高=红+0.22x
- IssueFormUI.SetPriority 里同步调用
- 验证：手机上点绿按钮 Marker 变绿+变小，点红按钮变红+变大

---

## P-013

- 目标：Marker 颜色没变

### Prompt 原文

> marker并没有事实变色

### 结果

- **采用**
- 根因：三个优先级按钮的 onClick listener 是用 execute_code 绑的匿名 lambda，不被 Unity YAML 序列化 → 构建 APK 后 listener 全没了
- 验证：FindPriorityButtons() 里运行时重新绑 onClick（int idx=i capture + AddListener）

---

## P-014

- 目标：提交报"网络错误0"

### Prompt 原文

> 报告提交不上去显示网络错误0

### 结果

- **采用（踩坑）**
- 初判：adb reverse 丢了（重装 APK 清端口转发）→ 重新 reverse → 暂恢复
- 验证：手机 curl localhost:8080 → HTTP=000；重新 reverse 后 HTTP=200

---

## P-015

- 目标：还是网络错误0

### Prompt 原文

> 还是网络错误0

### 结果

- **采用（方案变更）**
- 根因：Unity 的 libcurl 不走 adb reverse 的端口转发
- 验证：手机 curl WiFi IP 192.168.31.127:8080 → 200 OK；URL 改 WiFi IP

---

## P-016

- 目标：变成一直"提交中"

### Prompt 原文

> 现在又变成了一直提交中

### 结果

- **采用**
- 根因：Unity PlayerSettings `insecureHttpOption: 0`（NotAllowed）**静默拦截 HTTP 请求**不回调
- 验证：logcat → `Non-secure network connections disabled in Player Settings`；改 ProjectSettings.asset insecureHttpOption: 2（AlwaysAllowed）+ 重建 APK

---

## P-017

- 目标：确认网络方案

### Prompt 原文

> 现在需要链接数据线还是只需要wifi链接同一个就能成功提交

### 结果

- **确认**
- 验证：当前走 WiFi IP 方案，手机和 PC 连同一个路由器即可，USB 线不需要。前提：PC IP 不变、后端监听 0.0.0.0:8080

---

## P-018

- 目标：提交新版本

### Prompt 原文

> 现在把这个新版本git上去

### 结果

- **采用**
- commit 27f3048：半透明平面、Marker 变色、WiFi 直连、HTTP 放开

---

## P-019

- 目标：上传演示视频到 GitHub README

### Prompt 原文

> 把这两个视频也上传到GitHub上的readme.md

### 结果

- **采用**
- 视频拷到 demos/ 目录，README 加 HTML5 `<video>` 标签
- commit 95a0716

---

## P-020

- 目标：视频在 README 里看不见

### Prompt 原文

> 上传的视频没看见啊

### 结果

- **采用（方案变更）**
- 根因：GitHub Markdown 渲染器**剥离 `<video>` HTML 标签**，不支持内嵌播放
- 验证：改用 Markdown 链接指向 `raw.githubusercontent.com` 直链，浏览器点击后自动播放
- commit 190bd49
