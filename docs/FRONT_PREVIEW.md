# Front 入口（已从预览切换为真实宿主）

当前架构、模块调用边界、Windows 编译/回归和真实操作警告见 [Front 真实模块接入](dcg/FRONT_RUNTIME.md)。

```powershell
.\scripts\build-windows.ps1 -Frontend Front
```

输出：`artifacts/windows/front/Release/build/MCUGLauncher.Front.exe`。独立工程为 `src/MCUGLauncher/Front/Front.csproj`，历史 tools/FrontPreview 工程已移除。
必须保留整个输出目录和依赖文件；自身 Styles/Theme.xaml 已编译进 Front 程序，不再需要 Fixtures。

Front 默认读取 `%LOCALAPPDATA%\BlockLaunch` 正式配置，共用账号、实例和持久化下载队列，启动恢复后会实际执行未暂停的下载。
新旧入口不能同时打开同一配置目录。新前端直接组装服务，不通过隐藏的 MainWindow 执行业务。

已接入账号、版本与加载器、启动/正常停止、资源浏览与下载、本地内容读取/启停、设置、任务列表与运行日志。
更新/事务回退、实例卸载、个性化等旧页面尚未完整迁移，不能整体删除旧 UI。

旧隔离预览的 `.front-preview` 标记检查、模拟资源、`--live` 和 `--simulate-game-running` 参数已经移除。
`--settings-dir <目录>` 仅指定真实配置位置，不禁止联网、下载、登录或游戏启动，也不自动把游戏根目录改到该位置。

本次尚未在 Windows 编译或运行。历史设置预览报告仅描述历史源码，不代表当前宿主通过测试。
