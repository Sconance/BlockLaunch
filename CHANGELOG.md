# 更新记录

## 0.20.1

- 重整下载页分类和资源详情的信息层级，详情页支持窄窗口排列。
- 加载器入口按卡片分组，本地安装器折叠收纳。
- 更新透明背景 Logo 和 EXE 图标，分类图标保留 ImageSource 图片接口。


近期发布摘要如下，完整历史见 [Releases](https://github.com/Sconance/MCUGLauncher/releases)。源码从 0.18.3 基线提交到主分支，未改写旧标签或重建历史提交。

## [0.20.0](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.20.0)

- 统一 C#、XAML 和项目文件排版，展开密集语句，便于阅读和审查。
- 拆分主页账号面板构建职责，将账号切换按钮样式移入 XAML。
- 整理下载任务卡片、状态排序和任务栏进度映射，明确任务状态变更入口。
- 将持久化任务模型、单次执行上下文与队列协调器分文件组织，保留数据和调用兼容。
- 新增开发导览、编码约定与 SDK 格式检查工具。
- 继续提供完整版与 Lite，保留原有配置、账号和实例兼容。

## [0.19.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.2)

- 拖入文件显示导入提示；整合包先确认名称和目标目录，其他资源导入管理页当前实例。
- 顶栏下载进度、任务栏进度与内嵌下载列表，支持暂停、继续、取消和失败重试。
- 下载期间保留账号切换和版本浏览；认证操作先提示暂停下载，安装与启动仍保持互斥。

## [0.19.1](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.1)

- 修订：修复启动按钮右侧展开与设置图标被公共按钮内边距遮挡的问题。

- 启动页移除 Java 筛选与搜索图标，新增搜索占位提示。
- 大启动按钮搭配纵向展开、设置按钮。
- 版本内容管理增加目录切换，标题整合到顶栏，列表和滚动条更紧凑。
- 强化导入区域分隔，修复内嵌页面切换深色主题时的背景。

## [0.19.0](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.19.0)

- 最终名称确定为 MCUG Launcher，统一界面、程序元数据、仓库与文档。
- 源码项目目录和构建入口同步更名，保留现有账号、配置与实例兼容。

## [0.18.3](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.18.3)

- Java 指定目录、全盘搜索、取消、筛选与路径校验；自动匹配包含游戏 runtime。
- 统一版本与加载器识别，修复旧式 NeoForge 分类。
- 首页 Java 筛选、下载失败重试与整合包清单预检。
- 本地背景列表、填充、强度和强调色设置。

## [0.18.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.18.2)

- 启动页和公共样式现代化。
- 修订最大化拖动恢复与布局适配。

## [0.18.1](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.18.1)

- 按反馈调整目录、版本列表、搜索和导航布局。
- MCUG 徽标作为应用图标。

更早版本请参阅对应发布说明。
