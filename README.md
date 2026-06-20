# SmartTimer - 智能定时提醒器

整合granite、eclipse、falcon、helix四个模型优点的智能定时提醒工具。

## 功能特性

### 定时类型
- **一次性定时** — 在指定时间点触发一次后自动结束
- **周期性定时** — 按设定间隔（分钟）循环提醒
- **每日定时** — 每天在固定时间触发
- **每周定时** — 指定星期几触发（支持多选）

### 提醒方式
- 弹窗提醒（支持稍后提醒/Snooze）
- 声音提醒（系统默认音 或 自定义 .wav 音频文件）
- 托盘气泡提示

### 管理功能
- 添加 / 编辑 / 删除任意数量的定时
- 启用 / 禁用单个定时
- 定时列表实时显示下次提醒时间
- 即将触发的定时预览（最多5个）

### 快捷操作
- 右键托盘图标 → 快速添加常用时长（5/10/15/30/60 分钟）
- 双击托盘图标打开管理窗口
- 左键单击托盘图标切换全局静音

### 其他功能
- 开机自启（注册表写入）
- 全局静音（静音状态图标可视化）
- 数据自动保存（JSON 文件，存于 `%AppData%\SmartTimer\`）
- 单实例运行（防止重复启动）

## 系统要求

- Windows 10 / Windows 11
- .NET 8 Desktop Runtime（若使用框架依赖部署）

## 编译方法

### 前置条件
安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 编译命令

```bash
# 框架依赖部署
dotnet build -c Release

# 发布单文件版本
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

## 使用说明

1. 运行 `SmartTimer.exe`
2. 程序会在系统托盘显示一个时钟图标
3. **右键**托盘图标 → 选择「打开定时管理」打开管理窗口
4. 点击「添加」创建新的定时提醒
5. 也可以使用「快速添加」快速创建常用时长的一次性提醒
6. **双击**托盘图标也可打开管理窗口
7. **左键单击**托盘图标切换全局静音

## 数据文件

所有数据保存在 `%AppData%\SmartTimer\` 目录下：
- `settings.json` — 全局设置和定时配置

## 项目结构

```
SmartTimer/
├── SmartTimer.csproj           # 项目文件
├── Program.cs                  # 程序入口
├── Models/
│   ├── TimerItem.cs            # 定时模型
│   └── AppSettings.cs          # 设置模型
├── Services/
│   ├── TimerEngine.cs          # 定时调度引擎
│   ├── SettingsManager.cs      # 设置管理器
│   ├── SoundService.cs         # 声音播放服务
│   └── AutoStartService.cs     # 开机自启服务
└── UI/
    ├── TrayApplicationContext.cs # 托盘上下文
    ├── ManagerForm.cs           # 管理窗口
    ├── EditTimerForm.cs         # 编辑窗口
    └── ReminderForm.cs          # 提醒弹窗
```

## 技术实现

- **开发语言**：C#
- **UI框架**：Windows Forms (WinForms)
- **目标框架**：.NET 8.0 Windows
- **定时引擎**：基于 System.Windows.Forms.Timer，每秒检查一次触发点
- **数据持久化**：System.Text.Json 序列化到本地文件
- **单实例**：Mutex 互斥体
- **开机自启**：注册表 Run 键

## 整合的优点

| 来源 | 整合的优点 |
|------|-----------|
| granite | 四种定时类型、动态时钟图标、即将触发列表 |
| eclipse | 清晰的代码结构、编译零错误、稍后提醒功能 |
| falcon | 服务化架构、静音功能、静音状态可视化图标 |
| helix | 位运算周几匹配、日切重置机制、Snooze临时定时 |

## License

MIT
