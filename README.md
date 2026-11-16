# Reminder30

Reminder30 是一款 Windows 定时提醒工具。启动后默认每 30 分钟提醒一次，支持将提醒间隔设置为 1–240 分钟。

## 运行要求

- Windows 系统
- .NET Framework 4.8

## 使用方法

1. 运行 `Reminder30.exe`。程序会自动开始 30 分钟倒计时，并最小化到系统托盘。
2. 双击托盘图标，或右键选择“显示窗口”，可查看倒计时。
3. 如需调整间隔，先点击“停止”，将分钟数改为 1–240，再点击“开始”。托盘菜单也提供开始和停止操作。
4. 时间到后，程序会显示提醒窗口、播放系统提示音并显示托盘通知。提醒结束后会继续按所选间隔计时。

关闭主窗口只会将程序隐藏到系统托盘；要完全退出，请右键托盘图标并选择“退出”。

## 从源码构建

需要安装 Visual Studio 或 Visual Studio Build Tools，以及 .NET Framework 4.8 开发目标包。在本目录执行：

```powershell
msbuild "Reminder30.sln" /p:Configuration=Release
```

构建完成后，程序位于 `bin/Release/Reminder30.exe`。
