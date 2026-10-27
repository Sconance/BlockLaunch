<p align="center"><img src="src/MCUGLauncher/Assets/MCUGLogo.png" width="112" alt="MCUG Launcher Logo"></p>
<h1 align="center">MCUG Launcher</h1>
<p align="center">面向 Windows 的 Minecraft Java 版启动器</p>
<p align="center"><a href="https://github.com/Sconance/MCUGLauncher/releases/latest">下载最新版</a> · <a href="docs/USAGE.md">使用指南</a> · <a href="https://github.com/Sconance/MCUGLauncher/issues">反馈问题</a> · <a href="CHANGELOG.md">更新记录</a> · <a href="compare_develop_refactor.md">develop/refactor 功能对照</a></p>

MCUG Launcher 提供游戏启动、分类下载、账号登录和实例内容管理，采用 MCUG 蓝白主题，支持浅深配色与本地背景。升级保留现有账号、配置和实例数据。

## 界面预览

![启动与版本列表](docs/images/home.png)

![版本内容管理](docs/images/manager.png)

<details><summary>下载任务与整合包确认</summary>

![下载任务](docs/images/download-tasks.png)
![整合包导入](docs/images/pack-import.png)

</details>

<details><summary>Java 管理与个性化</summary>

![Java 管理](docs/images/java.png)
![个性化](docs/images/appearance.png)

</details>

截图来自隔离测试环境，实例与账号信息仅用于演示。

## 功能

| 方向 | 已有能力 |
| --- | --- |
| 账号与启动 | 微软正版、离线角色、MUA 认证，头像与会话刷新 |
| 游戏下载 | 原版分类、加载器版本配置、整合包及资源分类下载 |
| 资源中心 | Modrinth 搜索、兼容筛选、排序、收藏筛选、卡片/列表视图、项目介绍与依赖详情 |
| 实例管理 | 多目录、隔离、分类、搜索、直接启动、启动参数与内容管理 |
| 整合包 | Modrinth、MultiMC / Prism 导入，清单预检与可选内容导出 |
| Java | 常见位置扫描、指定目录与全盘搜索、手动指定、版本匹配 |
| 下载任务 | 紧凑进度与速度、暂停继续、失败重试、任务恢复 |
| 扩展入口 | 顶部、下载、设置、更多、主页、资源详情、版本设置和实例内容八个菜单区 |
| 辅助工具 | 存档备份、截图预览、日志、崩溃线索与诊断导出 |

当前资源平台以 Modrinth 为主，CurseForge 在线服务未接入。蓝图入口浏览实例的 schematics 目录。社区、在线公告及新闻尚未实现。

## 下载与运行

在 [Releases](https://github.com/Sconance/MCUGLauncher/releases/latest) 下载对应 EXE，直接运行。完整版文件名保持 Windows-x64.exe，内置 .NET 运行库；精简版文件名以 Windows-x64-Lite.exe 结尾，需要电脑安装 .NET 8 Desktop Runtime x64。游戏需匹配版本的 Java；内嵌微软授权页面需要 WebView2 Runtime。

升级前关闭启动器，再替换 EXE，保留原有配置和游戏目录。删除版本前检查确认框，重要存档先备份。

## 源码与构建

当前版本为 **0.20.2**，最终产品名确定为 **MCUG Launcher**。使用 Windows x64 与 .NET 8 SDK：

```powershell
git clone https://github.com/Sconance/MCUGLauncher.git
cd MCUGLauncher
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet publish src/MCUGLauncher/MCUGLauncher.csproj -c Release -o artifacts/publish
```

详见 [构建与目录说明](docs/BUILD.md)。历史版本源码仍保留在各 Release 的手动 Source.zip 附件中；旧标签未移动，旧版自动生成的 Source code 归档可能仅含原仓库文件。

### 窗口组件索引

窗口 partial 和独立组件按职责归档；每个类/记录类型均有单独说明。查看[组件目录与逐类文档](docs/MainWindow/README.md)。

重构分支变更记录见 [update_refactor.md](update_refactor.md)，后续拆分与主分支整合计划见 [TODO.md](TODO.md)。

```text
src/       主程序与核心逻辑（2 个项目）
test/      功能、界面与集成检查（22 个项目）
tools/     隔离界面预览（4 个）和源码格式工具
scripts/   构建与检查脚本
docs/      使用、构建、历史测试记录与截图
```

## 反馈与贡献

请在 [Issues](https://github.com/Sconance/MCUGLauncher/issues) 提供软件版本、系统版本、复现步骤和脱敏日志。不要上传密码、Token、账号缓存或私人存档。参阅 [贡献说明](CONTRIBUTING.md)。

## 近期更新

### [0.20.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.20.2)

- 资源收藏增强、兼容筛选、排序及卡片/列表布局。
- 为顶部、下载、设置、主页、资源详情、版本设置和实例内容等区域提供源码级菜单扩展点。

### [0.20.0](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.20.0)

源码可读性整理：统一格式，拆分账号面板与下载任务职责，加入开发导览和格式检查。

### [0.19.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.2)

- 拖入文件显示导入提示；整合包先确认名称和目标目录，其他资源导入管理页当前实例。
- 顶栏下载进度、任务栏进度与内嵌下载列表，支持暂停、继续、取消和失败重试。
- 下载期间保留账号切换和版本浏览；认证操作先提示暂停下载，安装与启动仍保持互斥。



### [0.19.1](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.1)

- 启动页移除 Java 筛选与搜索图标，新增搜索占位提示。
- 大启动按钮搭配纵向展开、设置按钮。
- 版本内容管理增加目录切换，标题整合到顶栏，列表和滚动条更紧凑。
- 强化导入区域分隔，修复内嵌页面切换深色主题时的背景。



### [0.19.0](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.0)

- 最终名称确定为 MCUG Launcher，统一界面、程序元数据、仓库与文档。
- 源码项目目录和构建入口同步更名，保留现有账号、配置与实例兼容。



### [0.18.3](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.18.3)

- 完善 Java 发现、目录搜索、取消与自动匹配。
- 统一版本识别，修复旧式 NeoForge 分类。
- 首页 Java 筛选、下载失败重试、整合包预检。
- 本地背景、填充方式、图片强度与主题强调色。

### [0.18.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.18.2)

- 启动页与公共 UI 样式现代化。
- 修订最大化后的拖动恢复与布局适配。

[完整更新记录](CHANGELOG.md) · [全部历史版本](https://github.com/Sconance/MCUGLauncher/releases)

MCUG Launcher 为独立项目，非 Mojang 或 Microsoft 官方产品。第三方组件见 [依赖清单](docs/DEPENDENCIES.md) 与程序内声明。项目整体许可证尚未指定，不代表自动授予任意使用或再分发许可。


开发者可从 [开发导览](docs/DEVELOPMENT.md) 查看功能入口、模块边界与编码约定。
