# 双前端 Windows 构建与验收交接

## 1. 状态与限制

本轮在 `refactor-dcg` 继续补充 Front 功能，包括资源实例安装与依赖解析、内容更新/事务恢复、实例卸载、Java 扫描/全盘搜索与保存、存档备份/恢复/导入、截图、诊断导出、资源收藏、整合包升级副本、账号皮肤头像、资源卡片/列表切换和 Front 菜单扩展注册入口。
本轮增量按用户要求**未在开发端编译、运行或做本地测试**，以前的构建或测试结果不证明当前提交通过。
`test/` 全部测试源码和 `scripts/run-checks.ps1` 已删除，不要执行旧文档中的测试项目命令。

技术栈仍是 C# / .NET 8 / WPF / XAML，不是 Tauri、Vue 或 React。
Front 是真实业务宿主；旧 UI 仍保留，尚不能宣称视觉或功能完全等价，也不要删除旧 UI。
在线头像、旧扩展菜单、资源卡片/列表切换和像素级样式仍有差异；资源现在支持选择实例并解析依赖后加入安装队列，也保留外部目录下载。验收时明确记录，不能将缺失入口标为通过。

## 2. 记录环境并构建

Windows x64，SDK 使用 `global.json` 的 8.0.426。记录以下输出及每条命令的退出码：

```powershell
git branch --show-current
git rev-parse HEAD
git status --short
dotnet --info
python --version
python scripts/check-layout.py
python scripts/check-frontend-boundaries.py
.\scripts\build-windows.ps1 -Frontend Both
.\scripts\build-windows.ps1 -Frontend Both -Publish
python scripts/check-frontend-boundaries.py --build
```

NuGet 还原失败与源码编译失败分开记录；不要擅自升级依赖或降低目标框架。
最后一项在临时副本中验证：删 Front 后旧入口可构建，删旧 UI 后 Front 可构建。
编译错误先修复本轮引入部分，随后重新构建两个入口。界面验收只能在构建成功后进行。
构建输出详见 [BUILD.md](../BUILD.md)。两个入口不能同时使用同一配置目录。

## 3. 安全的临时数据准备

仅使用可丢弃目录。新配置默认仍可能指向正式游戏目录，因此**必须在首次启动前写入临时 Root**。
下面两个前端顺序使用同一临时配置，便于核对互通；每次切换先完全退出另一个入口。

```powershell
$testDir = Join-Path $env:TEMP ("MCUG-Front-Acceptance-" + [guid]::NewGuid().ToString("N"))
$configDir = Join-Path $testDir 'config'
$gameDir = Join-Path $testDir 'game'
$downloadDir = Join-Path $testDir 'downloads'
New-Item -ItemType Directory -Force $configDir, $gameDir, $downloadDir | Out-Null
$settings = @{
    Root = $gameDir
    ResourceDownloadDirectory = $downloadDir
    PreferMicrosoft = $false
    PreferMua = $false
    Player = 'FrontTest'
}
$json = $settings | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText((Join-Path $configDir 'settings.json'), $json, [System.Text.UTF8Encoding]::new($false))
& .\artifacts\windows\front\Release\build\MCUGLauncher.Front.exe --settings-dir $configDir
```

首次启动确认界面显示的游戏目录确实是 `$gameDir`。若不是，立即退出并排查配置解析，不能继续下载或删除。
JSON 字段名使用上面的大写形式，与 `Settings/Preferences.cs` 中的 `SettingsStore` 一致；禁止回退到正式配置目录。
旧入口目前不接受 `--settings-dir`。只在 Windows Sandbox/独立测试账户等隔离环境验收旧入口；若无法隔离，旧 UI 回归标记“未测”，不要直接运行它触达正式配置。
在隔离系统里，可先将上面生成的设置复制到该系统自己的 `%LOCALAPPDATA%\BlockLaunch\settings.json`，再顺序使用默认配置启动两个入口以验收互通；此操作仅允许在可丢弃的测试系统执行。
只复制获授权的测试整合包/实例到临时目录。不得复制账号缓存，不使用正式下载历史或真实存档。
真实微软/MUA 登录、游戏下载和游戏启动必须获得设备所有者许可；缺许可标记“未测”。

## 4. 原生操作清单

每项记录“通过 / 失败 / 未测”、实际操作、截图或日志路径。任何未亲自执行的项目只能标记未测。

| 范围 | 操作及预期 |
| --- | --- |
| 双入口/配置 | 新旧入口分别打开；账号选择、设置和测试实例互通；相同配置第二入口应被拒绝，不损坏文件 |
| 窗口与导航 | 主页、账号、下载各分类、设置、版本管理、日志；返回、最大化、拖动、最小化和关闭；100%/150%/200% DPI，无关键按钮截断 |
| 设置 | 临时游戏目录、Java、内存、分辨率、高级启动、网络、限速、外观；非法输入拒绝，保存后重开恢复；运行/忙碌门禁不失效 |
| 账号 | 离线名字保存；获许可后测试微软/MUA 登录、取消、刷新、退出和多角色；令牌不出现在日志、脚本或导出包中 |
| 下载 | 原版、加载器、整合包和外部资源入队；暂停/继续/重试/取消/重开恢复；新旧界面顺序操作同一测试队列，不重复执行 |
| 资源中心 | 搜索/收藏/版本读取；选择实例后预览依赖与总大小，确认后加入安装队列；外部下载仍不安装到实例；不兼容、依赖冲突和网络失败有提示 |
| 版本基本管理 | 概览/分类/收藏/独立设置与全局设置、加载器安装、修复、Mod/资源包/光影/数据包导入与启停；仅操作测试实例 |
| 内容更新 | 维护页检查兼容正式版、选择更新入队；已最新/无兼容/网络错误有提示；依赖冲突拒绝，下载后刷新；事务恢复先确认，检查文件回退 |
| Java | 扫描与全盘搜索显示路径、主版本、架构和错误；有效选择写入实例或全局设置并重开核对；保存额外路径；不兼容/不可执行路径拒绝，失败不写设置 |
| 备份恢复 | 含 level.dat 的测试世界手动备份、列表/时间/世界显示；选世界恢复为新目录，原世界及备份不被覆盖；空世界、损坏 ZIP 与链接路径应安全拒绝 |
| 整合包升级 | 对已导入且独立隔离的测试实例选择新版 mrpack/ZIP；展示保留选项和冲突/缺少基线警告；确认创建升级副本，源实例不变；取消/关闭清理暂存，源变化后提交拒绝 |
| 实例卸载 | 确认框展示实际路径与共享数据保留策略；取消不删除；确认后关联待执行任务取消、实例列表刷新；共享依赖/越界/链接目录拒绝，重要数据不参与 |
| 克隆/导出 | 复制可选存档/截图/服务器列表，生成独立实例并选中；导出 ZIP 不含账号；cmd/ps1 脚本含正确入口与配置参数，不含凭据；中文/空格路径可用 |
| 启动/日志/退出 | 获许可后测试原版/加载器启动、停止、预检、实时日志与 latest.log；关闭等待任务与进程结束，不在提交文件时退出；无许可标未测 |
| 旧 UI 回归 | 顺序切换到旧入口重复核心操作，共享模块变化不破坏旧入口；不把 Front 构建成功当作旧入口回归成功 |

## 5. 结果与结束条件

在本节追加：提交哈希、是否包含本地修复、系统/SDK、命令与退出码、逐项结果、缺陷与修复文件、截图/日志、剩余阻塞。
本文件是保留的 Windows 交接与结果记录，不因清理文档而删除。
只有必测项全部完成并记录后才删除根目录 `AGENT.md`；有未测项继续保留。
不要自动提交或推送 Windows 修复，先向用户汇报。不要把删除测试源码当作功能通过，也不要重新添加测试项目。

当前状态：**待 Windows 构建与验收。**
