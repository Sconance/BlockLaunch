<p align="center"><img src="BlockLaunch/Assets/MCUGLogo.png" width="112" alt="MCUG Launch Logo"></p>
<h1 align="center">MCUG Launch</h1>
<p align="center">面向 Windows 的 Minecraft Java 版启动器</p>
<p align="center"><a href="https://github.com/Sconance/MCUGLaunch/releases/latest">下载最新版</a> · <a href="docs/USAGE.md">使用指南</a> · <a href="https://github.com/Sconance/MCUGLaunch/issues">反馈问题</a> · <a href="CHANGELOG.md">更新记录</a></p>

MCUG Launch 提供游戏启动、分类下载、账号登录和实例内容管理，采用 MCUG 蓝白主题，支持浅深配色与本地背景。项目原名 BlockLaunch，部分内部项目名与命名空间沿用旧名。

## 界面预览

![启动与版本列表](docs/images/home.png)

<details><summary>Java 管理与个性化</summary>

![Java 管理](docs/images/java.png)
![个性化](docs/images/appearance.png)

</details>

截图来自隔离测试环境，实例与账号信息仅用于演示。

## 功能

| 方向 | 已有能力 |
| --- | --- |
| 账号与启动 | 微软正版、离线角色、MUA 认证，头像与会话刷新 |
| 游戏下载 | 原版分类、模组加载器安装相关流程 |
| 资源中心 | Modrinth 模组、整合包、资源包、光影、数据包，收藏与标签 |
| 实例管理 | 多目录、隔离、分类、搜索、直接启动、启动参数与内容管理 |
| 整合包 | Modrinth、MultiMC / Prism 导入，清单预检与可选内容导出 |
| Java | 常见位置扫描、指定目录与全盘搜索、手动指定、版本匹配 |
| 下载任务 | 紧凑进度与速度、暂停继续、失败重试、任务恢复 |
| 辅助工具 | 存档备份、截图预览、日志、崩溃线索与诊断导出 |

当前资源平台以 Modrinth 为主，CurseForge 在线服务未接入。蓝图入口浏览实例的 schematics 目录。社区、在线公告及新闻尚未实现。

## 下载与运行

在 [Releases](https://github.com/Sconance/MCUGLaunch/releases/latest) 下载 Windows-x64.exe，直接运行。游戏需匹配版本的 Java；内嵌微软授权页面需要 WebView2 Runtime。

升级前关闭启动器，再替换 EXE，保留原有配置和游戏目录。删除版本前检查确认框，重要存档先备份。

## 源码与构建

主分支现包含 0.18.3 基线源码。使用 Windows x64 与 .NET 8 SDK：

```powershell
git clone https://github.com/Sconance/MCUGLaunch.git
cd MCUGLaunch
dotnet build BlockLaunch/BlockLaunch.csproj -c Release
dotnet publish BlockLaunch/BlockLaunch.csproj -c Release -o artifacts/publish
```

详见 [构建与目录说明](docs/BUILD.md)。历史版本源码仍保留在各 Release 的手动 Source.zip 附件中；旧标签未移动，旧版自动生成的 Source code 归档可能仅含原仓库文件。

## 反馈与贡献

请在 [Issues](https://github.com/Sconance/MCUGLaunch/issues) 提供软件版本、系统版本、复现步骤和脱敏日志。不要上传密码、Token、账号缓存或私人存档。参阅 [贡献说明](CONTRIBUTING.md)。

## 近期更新

### [0.18.3](https://github.com/Sconance/MCUGLaunch/releases/tag/v0.18.3)

- 完善 Java 发现、目录搜索、取消与自动匹配。
- 统一版本识别，修复旧式 NeoForge 分类。
- 首页 Java 筛选、下载失败重试、整合包预检。
- 本地背景、填充方式、图片强度与主题强调色。

### [0.18.2](https://github.com/Sconance/MCUGLaunch/releases/tag/v0.18.2)

- 启动页与公共 UI 样式现代化。
- 修订最大化后的拖动恢复与布局适配。

[完整更新记录](CHANGELOG.md) · [全部历史版本](https://github.com/Sconance/MCUGLaunch/releases)

MCUG Launch 为独立项目，非 Mojang 或 Microsoft 官方产品。第三方组件见 [依赖清单](BlockLaunch/DEPENDENCIES.md) 与程序内声明。项目整体许可证尚未指定，不代表自动授予任意使用或再分发许可。
