# 知识条目：Unity AR 移动端功能

**条目ID：** ENTRY-004  
**分类：** 移动端 / Unity AR  
**关键词：** Unity, AR Foundation, 平面检测, 标记放置, ARCore, ARKit, 真机  
**重要程度：** 高

---

## 功能概述
移动端运行在支持 AR 的真机上，通过摄像头检测真实环境中的水平平面，用户点击平面即可放置 AR 标记，并填写表单上报问题。

## 必做功能
1. 在支持 AR 的真机上运行
2. 检测真实环境中的水平平面
3. 点击检测到的平面，在命中位置放置一个简单标记
4. 填写并提交问题：
   - 标题：必填
   - 其余字段按基础数据结构要求填写

## 核心功能详解

### 1. AR 平面检测
- 启动应用后自动开启 AR 会话（AR Session）
- 配置 AR Plane Manager 进行水平平面检测
- 检测到的平面显示可视化效果（半透明网格/色块）
- 支持多个平面同时检测与追踪
- 平面随摄像头移动实时更新边界

### 2. 标记放置交互
- 监听屏幕单点触摸事件
- 使用 AR Raycast Manager 从触摸点发射射线
- 射线命中平面时，在命中点实例化标记 Prefab
- 标记物体锚定在平面上，随平面移动保持位置
- 标记样式：简单的立方体/圆柱体/图钉模型即可

### 3. 问题提交表单
- 标题（title）：文本输入框，必填，前端做非空校验
- 描述（description）：多行文本输入框，选填
- 优先级（priority）：下拉选择/按钮组，选项为 low / medium / high
- 提交按钮：点击后发送 POST 请求到后端
- 提交的坐标数据为当前放置标记的世界坐标（x, y, z）

### 4. 网络通信
- 使用 UnityWebRequest 或 HttpClient 发送 HTTP 请求
- 请求方式：POST，Content-Type: application/json
- 请求体包含：title、description、priority、position（x/y/z）
- 后端地址可配置（Inspector 中设置，方便真机测试）
- 提交成功/失败提示

## Unity 场景配置步骤
1. 创建 AR 基础场景：AR Session + AR Session Origin
2. 配置平面检测：AR Plane Manager，Detection Mode = Horizontal
3. 配置射线检测：AR Raycast Manager
4. 创建标记 Prefab：简单 3D 物体
5. 制作 UI 界面：UGUI 表单面板
6. 编写核心脚本：PlaneMarkerPlacer.cs + IssueSubmitter.cs

## 平台选择
- **Android 路线：** ARCore，需要 Android 7.0+ 设备，Android SDK
- **iOS 路线：** ARKit，需要 Mac + Xcode + iPhone（A9+）+ 开发者账号

## 来源
飞书文档《Unity-AR研发实操题-候选人任务书-简略版》第 3.1 节
