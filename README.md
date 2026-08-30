# CastoPet 项目结构

CastoPet 是一个基于 .NET 10 和 WPF 的 Windows 桌面宠物应用。本文件简要说明仓库中美术资源、应用代码和测试代码的目录职责。

## 顶层目录

```text
CastoPet/
├─ artwork/   美术制作、候选和参考素材
├─ src/       应用源代码与运行时资源
└─ tests/     自动化测试代码
```

## artwork：美术资源

```text
artwork/
├─ authoring/                 可编辑的美术工程源文件
│  └─ Castorice/              Castorice 的原图、拆分图层、表情目标和动画定义
├─ candidates/                等待审查或挑选的生成候选资源
│  └─ Castorice/              Castorice 的候选状态帧、表情图和预览图
├─ references/                美术制作使用的本地参考资料，不直接打包
│  ├─ character/              角色造型参考图
│  └─ expressions/            角色表情参考图
└─ README.md                  美术工作区的补充说明
```

`artwork/` 不直接参与应用打包。确认采用的资源需要显式整理到 `src/CastoPet/Assets/Runtime/`。

## src：应用代码

```text
src/
└─ CastoPet/                  WPF 应用项目
   ├─ Core/                   核心层：纯业务模型、状态和计算规则
   │  ├─ Animation/           宠物动作、帧时序和动画状态切换
   │  ├─ Diagnostics/         与技术实现无关的崩溃报告格式
   │  ├─ Input/               输入事件、手势识别和响应状态
   │  ├─ Movement/            宠物移动、光标推动和位置规划
   │  ├─ Product/             产品版本、功能配置和运行状态
   │  ├─ Settings/            设置模型、主题和设置项定义
   │  ├─ Shortcuts/           快捷项模型、类型和 URI 规则
   │  ├─ Skins/               皮肤、表情模型和资源限制
   │  └─ Wheel/               径向菜单模型、布局和选择规则
   ├─ Application/            应用层：组织用户用例和业务流程
   │  ├─ Diagnostics/         异常捕获协调和日志抽象
   │  ├─ Interaction/         宠物交互流程协调
   │  ├─ Menus/               菜单命令及其契约
   │  ├─ Settings/            设置事务、窗口设置和设置存储抽象
   │  ├─ Shortcuts/           快捷项管理、拖放和启动流程
   │  ├─ Skins/               当前皮肤选择流程
   │  ├─ Updates/             更新检查、下载和应用流程
   │  └─ Wheel/               径向菜单目录的组装与维护
   ├─ Infrastructure/         外部层：操作系统、文件和第三方组件实现
   │  ├─ Assets/              皮肤清单及图片资源的加载和写入
   │  ├─ Diagnostics/         日志文件和崩溃报告存储
   │  ├─ Persistence/         设置、应用路径和预览数据迁移
   │  ├─ Platform/            Windows 托盘、启动、输入钩子和窗口能力
   │  ├─ Shortcuts/           Windows 拖放数据读取
   │  └─ Updates/             Velopack 更新服务实现
   ├─ Presentation/           表示层：WPF 界面和界面相关服务
   │  ├─ Services/            WPF 菜单宿主等界面适配服务
   │  ├─ Shortcuts/           快捷项图标的界面展示支持
   │  ├─ Styling/             设置窗口和径向菜单样式
   │  └─ Windows/             宠物、设置和崩溃通知窗口
   ├─ Assets/                 应用打包资源
   │  └─ Runtime/             正式运行时皮肤清单与动画图片
   ├─ Properties/             程序集和项目属性
   ├─ App.xaml(.cs)           WPF 应用生命周期与依赖组装
   ├─ Program.cs              程序入口和 Velopack 初始化
   └─ CastoPet.csproj         .NET、WPF、依赖和资源打包配置
```

`bin/` 和 `obj/` 是编译产生的输出及中间目录，不属于源代码架构。

## tests：测试代码

```text
tests/
└─ CastoPet.Tests/            CastoPet 自动化测试项目
   ├─ Application/            应用层流程测试
   │  ├─ Diagnostics/         崩溃诊断流程测试
   │  ├─ Settings/            设置流程测试
   │  ├─ Skins/               皮肤选择流程测试
   │  └─ Updates/             更新流程与策略测试
   ├─ Architecture/           目录、命名空间和依赖边界测试
   ├─ Catalog/                各测试分组的注册目录
   ├─ Core/                   核心规则测试
   │  ├─ Animation/           打包动画与帧配置测试
   │  └─ Skins/               内置皮肤定义测试
   ├─ Harness/                自定义测试用例和运行器
   ├─ Infrastructure/         外部层实现测试
   │  ├─ Assets/              资源服务和皮肤清单测试
   │  └─ Platform/            平台与持久化行为测试
   ├─ Presentation/           WPF 表示层测试
   │  └─ Windows/             窗口结构和设置界面测试
   ├─ Support/                测试数据及通用断言工具
   ├─ Program.cs              测试程序入口
   ├─ TestSuite.Catalog.cs    测试套件总目录
   └─ CastoPet.Tests.csproj   测试项目配置
```

测试目录大体对应 `src/CastoPet/` 的代码分层，方便按职责定位实现及其验证代码。
