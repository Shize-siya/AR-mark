# AR 巡检系统 - Unity AR 移动端（Android + ARCore）

## 环境

- Unity 2022.3 LTS 或 Unity 6 LTS
- 模块：Android Build Support（含 OpenJDK、Android SDK & NDK）
- ARCore 支持的安卓真机（Android 7.0+，开启 USB 调试）

## 一、项目创建与 AR 包安装

1. Unity Hub → New Project → **3D (Built-in 或 URP 均可)** → 项目名 `ar-inspection-unity`
2. 把本目录 `Assets/Scripts/` 下的 3 个脚本复制到项目的 `Assets/Scripts/`：
   - `IssueApiClient.cs` — 后端 HTTP 通信
   - `PlaceMarkerOnPlane.cs` — 点击平面放置标记
   - `IssueFormUI.cs` — 上报表单逻辑
3. Window → Package Manager → Packages: Unity Registry，安装：
   - **AR Foundation**
   - **ARCore XR Plugin**
4. Edit → Project Settings → **XR Plug-in Management** → Install → 切到 Android 标签 → 勾选 **ARCore**

## 二、场景搭建

### 方式 A：一键自动搭建（推荐）

1. 确保已把本目录 `Assets/` 下的 **Scripts/**（3 个运行时脚本）和 **Editor/SceneBuilder.cs**（编辑器脚本）复制进项目
2. 等待 Unity 编译完成，菜单栏出现 **Tools → AR巡检 → 一键搭建 AR 场景**，点击它
3. 自动完成：AR Session、XR Origin + 平面检测、标记与平面 Prefab、表单 UI、脚本连线、场景保存（`Assets/Scenes/ARScene.unity`）、安卓构建设置（ARM64 / API 24 / OpenGLES3）
4. 弹窗提示后只需手动做两件事：
   - Project Settings → **XR Plug-in Management** → Android 标签 → 勾选 **ARCore**
   - 选中场景中的 **AppManager**，把 **Server Base Url** 改为电脑局域网 IP（如 `http://192.168.1.100:8080`）

> 如脚本报编译错误，通常是 AR Foundation / CoreUtils 包未装完，确认包安装后重新点击菜单即可。

### 方式 B：手动搭建（约 10 分钟）

1. 删除场景中默认的 `Main Camera`
2. Hierarchy 右键 → XR → **AR Session**
3. Hierarchy 右键 → XR → **XR Origin (AR Rig)**，给它添加组件：
   - **AR Plane Manager**（Plane Prefab 可先用默认的 Default Plane；Detection Mode 设为 Horizontal）
   - **AR Raycast Manager**
   - **Place Marker On Plane**（我们的脚本）
4. 创建标记 Prefab：Hierarchy 右键 → 3D Object → Cube，缩放设为 (0.1, 0.1, 0.1)，
   拖进 Project 窗口做成 Prefab，命名为 `Marker`，然后删掉场景里的 Cube。
   把 `Marker` 拖到 PlaceMarkerOnPlane 的 **Marker Prefab** 槽位
5. 创建空物体 `AppManager`，挂 **Issue Api Client** 脚本
6. 创建 UI：Hierarchy 右键 → UI → **Canvas**（Render Mode: Screen Space - Overlay），在 Canvas 下：
   - 右键 Canvas → UI → **Panel**，命名 `FormPanel`，拉伸铺满下半屏即可
   - Panel 内依次添加：**Input - Field**（命名 TitleInput，Placeholder 填"标题（必填）"）、
     **Input - Field**（DescriptionInput）、**Dropdown**（PriorityDropdown，Options 改为：低 / 中 / 高）、
     **Button - TextMeshPro 或 Legacy**（SubmitButton，文字"提交上报"）、**Text**（StatusText 提示条）
   - Canvas 上挂 **Issue Form UI** 脚本，把上面各控件、AppManager（Api Client）、XR Origin（Marker Placer）拖到对应槽位
   - PlaceMarkerOnPlane 的 **Issue Form** 槽位拖入 Canvas
7. 选中 `FormPanel`，在 Inspector 右上角取消勾选使其默认隐藏

## 三、Android 构建与真机部署

1. Edit → Project Settings → Player → Android 标签：
   - Company Name / Product Name 随便填
   - Other Settings → **Minimum API Level: 24 (Android 7.0)**
   - Other Settings → **Target Architectures: 勾选 ARM64，取消 ARMv7**（ARCore 要求）
   - Other Settings → **Graphics APIs：移除 Vulkan**（部分 ARCore 设备兼容问题），只保留 OpenGLES3
2. File → Build Settings → 选 **Android** → Switch Platform
3. 手机开启开发者模式 + USB 调试，数据线连接电脑
4. Build Settings → Refresh 选中你的设备 → **Build And Run**
5. 首次启动 App 会请求相机权限，允许即可；如提示安装 "Google Play 服务 for AR"，按提示安装

## 四、配置 Go 后端地址（真机联调关键）

1. 手机和电脑连**同一个 Wi-Fi**
2. 电脑启动 Go 后端（`go run .` 或 `server.exe`）
3. 电脑终端 `ipconfig` 查看 IPv4 地址，如 `192.168.1.100`
4. Unity 中选中 AppManager，把 **Server Base Url** 改为 `http://192.168.1.100:8080`
5. 若手机访问不通，检查 Windows 防火墙放行 8080 端口

## 五、功能验证清单

- [ ] 真机打开 App，移动摄像头后出现平面网格
- [ ] 点击平面放置立方体标记，表单弹出
- [ ] 标题留空点提交 → 提示"标题不能为空"
- [ ] 填标题提交 → 提示"提交成功"，表单收起
- [ ] 电脑浏览器打开 React 页面 → 刷新看到该问题
- [ ] React 中修改状态 → 成功
- [ ] 重启 Go 服务 → React 刷新数据仍在

## 已完成 / 已知问题

- 已完成：平面检测、标记放置、表单校验、上报后端
- 已知问题：重启 App 后历史标记不会恢复到真实物理位置（题目明确不要求）
