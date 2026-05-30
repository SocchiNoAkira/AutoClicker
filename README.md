# AutoClicker

轻量级 Windows 鼠标连点器，基于 C# WinForms + .NET 8 开发。打包为单个 exe，无需安装运行环境。

## 为什么要写这个

网上免费的连点器，要么被杀毒软件报毒，要么捆绑广告软件。这个项目完全开源，每一行代码都能看到，可以自己构建，清楚知道程序在你的机器上做了什么。

## 功能

- **可调点击频率**：1–1000 次/秒
- **持续时间控制**：设定秒数计时停止，或勾选"永久持续"直到手动停止
- **两种点击模式**：
  - *跟随光标*：鼠标在哪就点哪
  - *固定位置*：在屏幕上选取一个或多个坐标，程序轮流点击
- **屏幕选取位置**：点击"选取"按钮后，半透明覆盖层铺满屏幕——左键选点，右键取消，不用手动输入 X/Y 坐标。可多次选取添加多个位置
- **全局热键 F6**：任何应用中都能开始/停止，窗口不需要在前台
- **系统托盘**：最小化到托盘，右键菜单可开始/停止/退出
- **干净关闭**：关窗口立刻停止点击并退出，不会残留后台进程
- **覆盖层超时**：选取位置的界面如果 1 分钟无操作，自动关闭避免卡住

## 安装与构建

本项目不提供预编译的 exe 下载（自包含打包约 150MB，超出 GitHub 文件限制）。请按以下步骤自行构建。

### 前置条件

| 要求 | 说明 |
|---|---|
| 操作系统 | Windows 10 1607+ / Server 2016+ |
| 架构 | x64 |
| .NET SDK | 8.0 或更高版本 |
| 磁盘空间 | 约 1GB（SDK + 构建缓存） |

安装 [.NET 8 SDK (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)，然后验证：

```
dotnet --version
# 应输出 8.x.x
```

### 一键构建

```bat
git clone https://github.com/SocchiNoAkira/AutoClicker.git
cd AutoClicker
build.bat
```

构建完成后，`dist\AutoClicker.exe` 即为可执行文件，双击运行，无需额外安装 .NET Runtime。

### 手动构建

```bat
dotnet publish AutoClicker/AutoClicker.csproj ^
  -c Release -r win-x64 ^
  /p:PublishSingleFile=true ^
  /p:SelfContained=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  -o dist
```

| 参数 | 作用 |
|---|---|
| `PublishSingleFile` | 打包为单个 exe |
| `SelfContained` | 内含 .NET 运行时，用户无需额外安装 |
| `IncludeNativeLibrariesForSelfExtract` | 将原生 DLL 也打包进 exe |

如需缩小体积（约 1MB），可将 `SelfContained` 设为 `false`，但运行时需安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。

## 使用方法

1. **频率**：设定每秒点击次数（1–1000）
2. **时长**：设定持续秒数，或勾选"永久持续"
3. **位置模式**：
   - *当前鼠标位置*：点击跟随鼠标移动
   - *固定位置*：点击"选取"→ 屏幕变暗 → 左键添加位置（可重复添加）→ 右键取消
4. **开始/停止**：按 **F6** 或点击按钮
5. **最小化**：窗口最小化到系统托盘，双击托盘图标恢复

## 项目结构

```
AutoClicker/
├── build.bat                  # 一键构建脚本
├── README.md
└── AutoClicker/
    ├── AutoClicker.csproj     # 项目配置
    ├── Program.cs             # 入口
    ├── MainForm.cs            # 主界面 + 屏幕选取覆盖层
    ├── ClickerEngine.cs       # 连点引擎（BackgroundWorker + SendInput）
    ├── HotkeyManager.cs       # 全局热键 F6（RegisterHotKey）
    └── TrayManager.cs         # 系统托盘图标 + 右键菜单
```

## 技术说明

| 组件 | 实现方式 | 选择理由 |
|---|---|---|
| 点击模拟 | Win32 `SendInput` API | 微软推荐用以替代已弃用的 `mouse_event` |
| 全局热键 | Win32 `RegisterHotKey` | 无需安装全局键盘钩子，单次 API 调用，更干净 |
| 点击循环 | `BackgroundWorker` | 简单的间隔循环，不需要 async/await 的复杂度 |
| 位置选取 | 全屏半透明 `Form` 覆盖层 | 直接获取屏幕坐标，无需手动输入 X/Y |
| UI 框架 | WinForms | 轻量、启动快，工具类应用足够 |

## License

MIT
