<p align="center">
  <img src="src/CastoPet/Assets/Runtime/Castorice/Castorice.png" width="240" alt="CastoPet Castorice desktop pet">
</p>

<h1 align="center">CastoPet</h1>

<p align="center">
  一个轻量、安静的 Windows 桌面宠物，让 Castorice 陪在你的桌面上。
</p>

<p align="center">
  <a href="https://github.com/sunboming/CastoPet/releases/latest"><img src="https://img.shields.io/github/v/release/sunboming/CastoPet?display_name=tag&label=release" alt="Latest release"></a>
  <a href="https://github.com/sunboming/CastoPet/actions/workflows/build.yml"><img src="https://img.shields.io/github/actions/workflow/status/sunboming/CastoPet/build.yml?branch=main&label=build" alt="Build status"></a>
  <a href="https://github.com/sunboming/CastoPet/releases"><img src="https://img.shields.io/github/downloads/sunboming/CastoPet/total?label=downloads" alt="Total downloads"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2B-0078D4?logo=windows" alt="Windows 10 or later">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
</p>

<p align="center">
  <a href="#下载安装">下载安装</a> ·
  <a href="#主要功能">主要功能</a> ·
  <a href="#基本操作">基本操作</a> ·
  <a href="#开发">开发</a> ·
  <a href="docs/README.md">文档</a>
</p>

---

## 下载安装

CastoPet 面向 Windows x64，支持 Windows 10 及更高版本。

- [下载安装版 MSI](https://github.com/sunboming/CastoPet/releases/latest/download/CastoPet-win-Setup.msi)
- [下载便携版 ZIP](https://github.com/sunboming/CastoPet/releases/latest/download/CastoPet-win-Portable.zip)
- [查看全部版本与发行说明](https://github.com/sunboming/CastoPet/releases)

安装版支持选择安装范围和目录，并可通过 Windows 标准安装界面修改、修复或卸载。便携版解压后即可运行，程序数据保存在同目录的 `UserData` 中。

> CastoPet 当前未进行代码签名。首次下载或安装时，Windows 可能显示安全提示。请只从本仓库的 [GitHub Releases](https://github.com/sunboming/CastoPet/releases) 获取程序，并在继续前核对发布来源。

## 主要功能

- 内置 Castorice 桌宠角色。
- 8 帧待机动画与随机眨眼动画。
- 平滑的鼠标拖动，拖动期间自动暂停角色动画。
- 桌宠右键菜单与 Windows 系统托盘入口。
- 始终置顶、鼠标穿透、任务栏图标和开机启动设置。
- 单实例运行，重复启动时恢复已有桌宠窗口。
- 本地日志与崩溃报告，不自动上传用户数据。
- 安装版支持应用内检查和安装更新。

CastoPet 0.1 是精简的基础版本。表情轮盘、主动移动、快捷启动和输入响应等实验功能不包含在当前正式版本中。

## 基本操作

| 操作 | 效果 |
| --- | --- |
| 按住左键并移动 | 拖动桌宠 |
| 单击右键 | 打开桌宠菜单 |
| 单击系统托盘图标 | 恢复或显示桌宠 |
| 右键单击系统托盘图标 | 打开托盘菜单 |

桌宠菜单和托盘菜单使用同一套设置。开启鼠标穿透后，可通过系统托盘菜单重新关闭该选项。

## 数据位置

安装版将用户数据保存在：

```text
%LocalAppData%\CastoPet
```

便携版将用户数据保存在：

```text
<解压目录>\UserData
```

两个版本不会共享设置、日志或崩溃报告。移动便携版时，应同时移动其 `UserData` 目录。

## 开发

### 环境要求

- Windows 10 或更高版本
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PowerShell 7

### 构建与测试

```powershell
git clone https://github.com/sunboming/CastoPet.git
cd CastoPet

dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Debug
dotnet build CastoPet.sln -c Debug
```

Release 验证：

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Release
dotnet build CastoPet.sln -c Release
```

开发构建的可执行文件位于：

```text
src\CastoPet\bin\Release\net10.0-windows\CastoPet.exe
```

该文件不用于测试安装和自动更新。安装包和候选更新包必须通过仓库中的发布脚本生成。

## 项目结构

```text
CastoPet/
|-- artwork/                 美术工程源文件与候选资源
|-- docs/                    项目文档与发布约定
|-- eng/                     构建、校验、打包与发布脚本
|-- src/CastoPet/            WPF 应用与正式运行时资源
|-- tests/CastoPet.Tests/    自动化测试
`-- artifacts/               本地生成的构建和打包产物
```

只有 `src/CastoPet/Assets/` 中声明的资源会进入应用。`artwork/`、`artifacts/` 和 `tmp/` 不属于运行时资源来源。

## 文档

- [文档索引](docs/README.md)
- [分支与版本规则](docs/branches-and-releases.md)
- [发布流程](docs/releasing.md)
- [候选版本测试](docs/release-candidate-testing.md)
- [资源组织](docs/asset-organization.md)
- [已知风险](docs/known-risks.md)

当前 `main` 用于后续开发，`release/0.1` 用于 0.1.x 维护发布。历史完整版保留为只读恢复分支，具体边界以分支文档为准。
