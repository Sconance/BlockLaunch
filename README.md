# MCUG Launch

MCUG Launch 是面向 Windows 的 Minecraft Java 版启动器，采用蓝白配色与 MCUG 徽标，提供游戏启动、分类下载和实例内容管理。项目原名 BlockLaunch，现有仓库地址继续用于发布和历史版本归档。

## 下载与运行

- [下载最新版本](https://github.com/Sconance/BlockLaunch/releases/latest)
- [查看更新记录与历史版本](https://github.com/Sconance/BlockLaunch/releases)

在发布页的 Assets 中下载 `MCUGLaunch-版本号-Windows-x64.exe`，直接运行即可，无需解压。当前版本为 Windows x64 单文件程序；Minecraft 本身仍需要合适版本的 Java，启动器提供 Java 扫描、选择与自动管理功能。内嵌微软授权页面需要 WebView2 Runtime。

升级前关闭正在运行的启动器，再替换 EXE。已有账号、实例、收藏和任务继续沿用原配置目录。

## 主要功能

- **账号与启动**：微软正版、离线角色和 MUA 联合认证，显示角色头像，启动前验证与刷新在线会话。
- **游戏与资源下载**：原版版本分类、模组加载器安装，以及 Modrinth 模组、整合包、资源包、光影和数据包搜索下载。资源可保存到独立下载目录。
- **实例管理**：实例隔离、收藏与分类、启动参数设置、Mod 启用与禁用、更新检查，以及整合包导入和可选内容导出。
- **版本内容浏览**：存档、模组、资源包、光影、服务器入口、截图和蓝图目录；支持存档备份与截图预览。
- **下载任务**：全局进度与下载速度、暂停继续、失败重试和任务恢复。
- **辅助工具**：运行日志、崩溃线索分析、诊断反馈、Java 管理和服务器收藏。

首页左侧选择账号并启动游戏，点击启动按钮旁的 `>` 展开已安装版本列表，点击 `<` 收起。下载其他版本或资源请进入顶部“下载”；管理所选版本请使用齿轮入口。

## 源码与历史版本

各版本对应源码位于发布页手动附带的 `MCUGLaunch-版本号-Source.zip`；较早版本的附件可能保留 BlockLaunch 名称。GitHub 自动生成的 `Source code` 归档仅包含标签指向的仓库文件，请以发布页单独提供的 Source.zip 为准。

当前资源平台以 Modrinth 为主，CurseForge 在线服务尚未接入。蓝图分类目前用于浏览实例的 `schematics` 目录。具体版本的新增功能与验证范围请参阅对应发布说明。

MCUG Launch 为独立项目，非 Mojang 或 Microsoft 官方产品。
