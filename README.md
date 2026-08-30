# CastoPet

CastoPet 是一个基于 .NET 10 和 WPF 的 Windows 桌面宠物应用。

当前仓库从精简的 `0.1` 基线重新开始开发。公开的 0.1 版本只保留稳定、可验证的基础桌宠能力，不包含此前实验阶段的轮盘、表情、主动移动、快捷启动和输入响应功能。

## 当前功能

- Castorice 内置角色资源。
- 8 帧待机动画和 5 帧随机眨眼动画。
- 左键移动超过系统拖动阈值后拖动桌宠；拖动期间暂停动画。
- 桌宠右键菜单和 Windows 托盘菜单。
- 始终置顶、鼠标穿透、任务栏图标和开机启动设置。
- 单实例运行，重复启动时恢复已有窗口。
- 本地日志、崩溃报告和下次启动提醒。
- 基于 Velopack 和本仓库 GitHub Releases 的安装与更新基础设施。

## 分支

- `main`：后续功能开发的主分支，从 0.1 精简基线继续演进。
- `release/0.1`：0.1.x 维护和发布分支，只接收发布所需的修复、文档和版本调整。
- `codex/archive-main-before-0.1`：历史完整版 `main` 的只读恢复点。
- `codex/archive-release-0.1-history`：Git 历史清理前 0.1 开发提交链的只读恢复点。

分支职责、修复同步方向和版本发布规则见 [分支与发布说明](docs/branches-and-releases.md)。

## 目录

```text
CastoPet/
├─ artwork/                 美术工程源文件和候选资源，不直接打包
├─ docs/                    当前开发约定与历史说明
├─ eng/                     行尾检查、打包和发布脚本
├─ src/CastoPet/            WPF 应用代码及正式运行时资源
├─ tests/CastoPet.Tests/    自动化测试项目
├─ artifacts/               本地生成的安装包、报告和临时产物
└─ 项目问题.md              main 后续重构问题清单
```

只有 `src/CastoPet/Assets/` 中显式声明的资源会进入应用。`artwork/`、`artifacts/` 和根目录 `tmp/` 都不是运行时资源来源。

## 开发验证

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Debug
dotnet build CastoPet.sln -c Debug

dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Release
dotnet build CastoPet.sln -c Release
```

正常 Release 构建输出位于：

```text
src/CastoPet/bin/Release/net10.0-windows/CastoPet.exe
```

该目录中的 exe 是开发构建，不用于测试安装和自动更新。安装包必须通过打包脚本生成。

## 打包与发布

在干净的 `release/0.1` 工作树中构建 0.1 安装包：

```powershell
pwsh -NoProfile -File eng/package.ps1 -Version 0.1.0
```

创建 Tag、推送当前发布分支，并在本仓库生成 Draft Release：

```powershell
pwsh -NoProfile -File eng/release.ps1 -Version 0.1.0
```

发布脚本不会自动公开草稿。确认版本说明、Tag、安装程序、Velopack 文件和 `build-metadata.json` 后，再在 GitHub 页面手动发布。

## 文档

当前文档索引见 [docs/README.md](docs/README.md)。尚未解决的数据模型和动画控制器问题记录在 [项目问题.md](项目问题.md)，不应在 `release/0.1` 上进行大范围重构。
