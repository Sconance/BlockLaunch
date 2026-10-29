# 更新记录

## [0.21.0](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.21.0)

`test` 主干与 `refactor` 全量整合后的版本。结构以 `test` 的原生窗口 shell、九个独立 XAML 页面与组合根为准；`refactor` 的文档规则、版本控制与 0.20.1 / 0.20.2 功能内容全部并入，`refactor` 不再单独递增版本号。

- 合并 `refactor`（`db0ab6a`）：86 个 `MainWindow` partial 与 51 篇逐类文档按结构退役，历史内容保留在 `refactor` 分支，迁移去向见 [refactor 分支整合记录](docs/REFACTOR_INTEGRATION.md)。
- 版本号改为单一来源 `Directory.Build.props` 的 `McugVersion`，运行期统一读取 `ProductInfo.Version`。
- 修复诊断信息 `0.20.0`、导航区 `0.15.2`、关于区 `0.20.2` 与两处 User-Agent 的写死版本。
- 新增 `scripts/check-version.py` 与 CI 版本校验，发版标签、CHANGELOG、README 与程序集元数据必须一致。
- 新增 [版本号控制](docs/VERSIONING.md)、[refactor 分支整合记录](docs/REFACTOR_INTEGRATION.md)、[重构路线图](TODO.md) 与 [develop 与 refactor 功能对照](docs/COMPARE_DEVELOP_REFACTOR.md)，把整合与拆分检查要点沉淀为长期规则。
- 保留 `test` 的 Esc 关闭页内选择器、嵌套改名后实例列表与首页名称同步修复。

## [0.20.2](https://github.com/Sconance/MCUGLauncher/releases/tag/v0.20.2)

- 完善资源卡片和详情页的心形收藏，继续使用原有收藏数据。
- 增加跨搜索页的收藏筛选、兼容版本过滤、排序和卡片/列表切换。
- 统一八个菜单区域的栏目注册，支持图片图标、排序、页内导航和详情标签的延迟创建。
- 保留内置栏目，未注册的扩展不显示占位按钮；增加菜单开发示例。

## 0.20.1

- 重整下载页分类和资源详情的信息层级，详情页支持窄窗口排列。
- 资源搜索采用自适应多列卡片及带标签筛选，详情提供介绍、版本和依赖切换，并展示真实项目正文和截图。
- 加载器采用图标、说明、版本选择及安装按钮横向对齐的配置行，本地安装器折叠收纳。
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
