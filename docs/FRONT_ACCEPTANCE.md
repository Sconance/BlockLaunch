# Front 待测清单与旧 UI 删除条件

## 当前结论

**不能删除旧前端代码。本次不删除任何旧 UI。**

Front 目前具备外壳、模拟开始页、资源只读查询，以及五项启动设置的真实读写；其他页面是未迁移提示。
本轮设置页的 Windows agent 完整交接见 [设置页编译与验收指南](FRONT_SETTINGS_WINDOWS_TEST.md)，根目录 `AGENT.md` 是临时任务提示词，完成并记录测试后删除。
FrontPreview 默认使用独立测试配置目录，不读取正式账号或实例；设置写入仅作用于测试配置。
正式入口仍是 App.xaml 的 `StartupUri="MainWindow.xaml"`，不是 FrontWindow。
MainWindow 仍组装配置、账号、任务队列、游戏会话和界面控制器，多个组件依赖其字段和方法。
FrontPreview 还从原 App.xaml 读取共享样式，因此 App.xaml 也不能作为“旧样式”直接删除。

本文所有待测项均未验收；静态检查通过不代表编译、运行或视觉验收通过。
当前环境缺少 .NET SDK，且不是 Windows，不能执行 WPF 编译/运行验收。

## 1. 编译和隔离回归

在安装 global.json 指定 .NET SDK 8.0.426 的 Windows 环境，依次执行：

```powershell
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet build tools/FrontPreview/FrontPreview.csproj -c Release
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --content-update-controller
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --management
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front-settings
```

- [ ] 主项目与 FrontPreview 编译成功，无 XAML 资源、事件或类型错误。
- [ ] 账号/设置/队列/游戏会话服务检查通过：`dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --business-services`、`dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --queue-service`。
- [ ] Front 隔离检查通过：真实适配映射、查询快照、分页、乱序响应、取消、详情失败、空结果及窗口关闭。
- [ ] 内容更新控制器检查通过：确认/拒绝、目录快照、运行门禁、取消、指纹和事务回退。
- [ ] 版本/资源管理检查通过：在线合并、离线回退、实例隔离、资源目标限制及本地资源操作。

### 设置页专项（本轮必测，尚未执行 Windows 验收）

- [ ] `--front-settings` 退出码 0，出现 `ALL_FRONT_SETTINGS_CHECKS_PASSED`，相关回归检查通过。
- [ ] 设置页真实读取、合法保存、重启恢复、重新读取放弃编辑、保留其他偏好。
- [ ] 非法内存/尺寸/Java/目录拒绝保存，文件哈希不变；读取错误和保存错误可恢复。
- [ ] `--simulate-game-running` 拒绝保存；同目录第二进程拒绝启动；已有非空非预览目录拒绝接管。
- [ ] 原生键鼠、缩放/最小尺寸、导航切换和关闭窗口验收完成，证据已写入专项指南。

验收时记录操作系统、SDK 版本、实际命令、退出码和首个失败信息；不要在未运行前勾选。

## 2. 模拟界面待测

```powershell
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release
```

| 状态 | 操作 | 预期结果 |
| --- | --- | --- |
| [ ] | 打开预览并依次点击全部导航 | 主窗口正常显示；开始页、内容中心、启动设置可访问；其余页面明确提示未迁移，不展示可执行的真实业务按钮 |
| [ ] | 调整窗口到最小尺寸，检查 Windows 100% / 150% / 200% 缩放 | 导航、搜索栏、列表、详情和底部按钮可见，没有不可操作的裁切或重叠；视觉问题需记录修复 |
| [ ] | 查看开始页 | 玩家和实例明确标注模拟；启动按钮不可用，不触发游戏或登录；独立宿主会初始化测试配置，只有设置页保存会提交编辑 |
| [ ] | 内容中心清空关键词并搜索 | 加载状态可见，显示 32 个模拟项目中的前 15 项，第一页不可点上一页 |
| [ ] | 连续点击下一页到末页，再返回 | 页码范围与项目一致；末页不可继续；没有重复翻页或跳过项目 |
| [ ] | 搜索 `empty` | 显示明确空状态，无旧结果或可翻到不存在页的按钮 |
| [ ] | 搜索 `error`，随后清空再搜索 | 显示错误而不崩溃；重新搜索恢复正常，加载状态解除 |
| [ ] | 搜索/详情加载期间点击取消 | 解除加载状态，显示取消提示，无取消后的结果覆盖 |
| [ ] | 连续搜索不同关键词并快速切换项目 | 后发请求有效；旧请求不能覆盖新列表或详情 |
| [ ] | 搜索后编辑筛选但不提交，再翻页/读详情 | 使用已提交的搜索条件；点击搜索后才应用新筛选 |
| [ ] | 选择项目、清除选择、切换导航后返回 | 详情与选择一致；离开页面取消请求，无旧版本列表残留；取消后可手动重新搜索 |
| [ ] | 请求期间关闭窗口 | 正常退出，无未处理异常或后台请求引发的界面更新 |
| [ ] | 使用 Tab、Enter、方向键操作 | 输入和搜索可用，焦点可达关键控件，长标题/描述显示可读 |

模拟数据只用于状态和布局验收，不证明 Modrinth 兼容性过滤正确。

## 3. 真实只读查询待测

```powershell
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release -- --live
```

| 状态 | 操作 | 预期结果 |
| --- | --- | --- |
| [ ] | 搜索已知 Modrinth 项目 | 名称、描述、作者、下载量来自真实结果；网络错误可见且可重试 |
| [ ] | 分别选择模组、资源包、光影、数据包、整合包 | 请求类别符合选择；没有兼容版本时显示空详情 |
| [ ] | 设置游戏版本和加载器，再选择项目 | 兼容版本由现有 ResourceManagementService 返回，不绕过原兼容性逻辑 |
| [ ] | 切换排序、翻页并快速更换查询 | 参数与显示一致，旧请求不能覆盖新结果 |
| [ ] | 断网、恢复网络、取消与超时 | 显示错误或取消/超时状态；恢复网络后可重试；无无限加载 |
| [ ] | 比对预览前后的正式账号、实例和配置 | 资源浏览不下载、不安装、不登录、不删除；独立测试配置可由设置页保存，正式配置不受影响；网络缓存/系统行为不等同业务写入 |

预览工具使用独立 HttpClient，不读取旧网络偏好、代理配置或账号。
当前不支持项目图标/长描述、收藏管理、下载入队和安装；不要把缺失功能当作已验收。

## 4. 原启动器回归

先在临时配置和可丢弃的实例中验收，不能直接对重要实例测试卸载/恢复。

```powershell
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --executor
dotnet run --project test/WorkflowUiChecks/WorkflowUiChecks.csproj -c Release
```

- [ ] 正式启动仍打开原 MainWindow，版本、账号和配置正常；FrontPreview 不改变正式入口。
- [ ] 下载执行隔离检查及原更新选择/预览/确认界面检查通过。
- [ ] 原账号、游戏启动/停止、下载暂停/继续/取消与退出清理无回归。
- [ ] 原版本设置、实例卸载、内容更新/回退和存档保护无回归。

上述检查不是全量发布验收；真实认证、游戏启动、加载器安装、整合包和存档恢复仍需对应环境验证。

## 5. 删除旧 UI 前必须满足的条件

| 状态 | 条件 |
| --- | --- |
| [ ] | Front 已迁移需要保留的功能：账号、版本/实例、启动/停止、下载队列、本地内容、更新、整合包、设置、日志诊断和存档保护；不再是占位页 |
| [ ] | 原窗口里的业务协调和共享状态已由服务/适配器接管，Controllers/Presentation 不再依赖待删除的字段、控件或静态 UI 工厂 |
| [ ] | 共享样式、资源、App 启动和依赖组装已明确迁移到新宿主，正式入口切换 Front 后正常工作 |
| [ ] | 所有引用、测试源码链接、反射入口、资源 URI、文档均完成迁移，编译与所需自动化/手工回归通过 |
| [ ] | 按页面逐批删除并验证，保留必要迁移兼容层；清楚区分纯 UI 与仍有业务职责的代码 |

不能直接删除 `MainWindow.xaml`、`MainWindow/UI/`、`MainWindow/Builders/`、`Presentation/` 或 App.xaml。
Builders 中还包括脚本和离线皮肤包生成，不能按目录名判断全是可删除 UI。
当前没有已确认可删除的整套旧页面；本轮新增设置页不代表完整设置和其他功能均已迁移，不删除旧源码。

## 验收记录

| 日期 / 环境 | 项目 / 命令 | 结果与证据 |
| --- | --- | --- |
| 待填写 | 待填写 | 尚未执行 Windows 编译、自动化运行与界面验收 |
