# 验证记录

## 当前提交 — 待 Windows 验收

测试项目和旧测试脚本已按要求删除。以下内容仅为既往版本的历史记录，命令不再适用，不证明本轮增量通过。
当前双前端构建、临时数据准备和验收步骤见 [Windows 交接](dcg/FRONT_UI_WINDOWS_TEST.md)，实际结果写入该指南末尾。

## 0.21.0 — 2026-10-28

- Release 构建：0 警告、0 错误；源码格式检查通过；29 个项目的引用与资源路径检查通过。
- 21 个检查程序通过：Auth、Mua、Collection、ContentSafety、Dependency、Diagnostic、Download、
  MiniDownload、Queue、ResourceDownload、Library、Stability、Uninstall、Upgrade、Experience、
  VersionUi、WorkflowUi、Compatibility、InlineUi、DownloadUi、CompactUi。
- 本次新增覆盖：资源详情页内切换与窄/宽布局、心形收藏与仅收藏筛选、搜索排序、卡片/列表视图、
  加载器版本读取与下载队列、八个菜单区域的注册/去重/延迟创建、下载扩展保留分类栏。
- CompactUi 使用真实版本清单与 Modrinth 搜索验证响应式卡片列数与独立资源下载。
- 未执行 `IntegrationChecks`（需要已安装游戏、Java 与真实启动矩阵）、真实微软/MUA 授权与原生鼠标/多显示器 DPI 验收。

## 0.20.0 — 2026-10-27

- Release 构建：0 警告、0 错误；源码格式检查通过；29 个项目的引用与资源路径检查通过。
- 21 个检查程序通过：Auth、Mua、Collection、ContentSafety、Dependency、Diagnostic、Download、
  Library、ResourceDownload、Stability、Uninstall、Upgrade、Experience、DownloadUi、VersionUi、
  CompactUi、InlineUi、MiniDownload、Queue、Compatibility、WorkflowUi（均为对应的 `*Checks` 工程）。
- 覆盖模拟认证与凭据保护、下载续传/取消、事务回滚与恢复、实例删除边界、资源依赖、
  整合包导入升级、账号显示和内嵌任务交互；CompactUi 另完成真实版本清单、Modrinth 搜索和独立资源下载。
- 更新 CompactUi 的异步等待与当前主页断言；更新版本断言并消除 DiagnosticChecks 的可空警告。
- 格式化前后以 C# token 比对保护表达式和字符串，任务类型分文件后保持同一命名空间和公开接口。
- 未执行需要真人凭据的真实登录，也未重跑完整真实游戏/整合包启动矩阵；组件渲染不等于原生鼠标与多显示器 DPI 验收。

以下保留历史记录，不能作为当前版本的全部验证结论。

## 历史记录 — 2026-10-20

## 0.2 增量验证

- Release 编译及 Windows x64 自包含发布：0 警告、0 错误。
- 真实 Modrinth 搜索与 Minecraft 1.21.1 / Fabric 版本过滤：通过。
- Sodium Extra 0.9.4 与必需依赖 Sodium 0.8.13：解析、下载、文件大小与 SHA-512 校验通过。
- 模组禁用、重新启用、重复安装不留重复文件、移至备份：通过。
- 错误哈希不改变已安装实例、提交中途失败回滚、路径穿越文件名拒绝：通过。
- 资源包搜索及 Fresh Animations 下载、大小 / SHA-512 校验、安装清单登记：通过。
- Quilt 0.30.1 / Minecraft 1.21.1：稳定版选择、真实安装与启动命令构建通过，未实际进入 Quilt 游戏。
- 已检查新版首页、导航、内容搜索与自定义下拉菜单；补齐了控件可访问性，修正了初始页面选择。
- 新安装模组及资源包的实际游戏效果、Forge / NeoForge 官方安装器交互、微软登录尚未在此轮实测。

## 0.1 基础链路验证

环境：Windows x64、.NET SDK 8.0.426、CmlLib.Core 4.1.0、Java 21。

| 项目 | 结果 |
| --- | --- |
| Release 自包含发布 | 通过，0 警告 / 0 错误 |
| 发布版窗口、首页、设置页 | 已打开并检查，修正文字对比度与按钮布局 |
| 官方版本列表 | 成功取得 920 条记录 |
| 原版 1.21.1 下载 | 4362 项文件任务完成 |
| 原版 1.21.1 离线账号启动 | 进程启动并持续运行 25 秒，随后测试程序关闭游戏 |
| 独立实例参数 | 启动参数包含指定实例路径 |
| 网络请求被本地拒绝时构建缓存版本启动命令 | 通过；修正为使用 LocalJsonVersionLoader |
| 断网条件下自动选择已缓存 Java | 通过 |
| Fabric 0.19.5 / Minecraft 1.21.1 | 安装成功，启动命令构建成功 |
| 微软正版登录、正版服务器连接 | 未实测，需要用户本人拥有游戏并完成授权 |
| Fabric 游戏实际运行、模组兼容性 | 未实测 |
| 全部历史 / 最新游戏版本 | 未覆盖，不能据此承诺全部版本兼容 |

离线账号运行时，游戏日志出现在线账号属性请求 401，游戏进程仍持续运行；这不表示离线账号可以使用正版在线服务。测试验证了本地启动链路，没有测试创建存档、游玩和所有硬件驱动组合。

现有版本是开发预览版，不等同于 PCL / HMCL 的完整功能集；缺失项目见 README。
