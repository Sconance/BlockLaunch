# MainWindow

`MainWindow` 是 WPF 窗口根类与组合根。该类型跨多个 `partial` 文件实现：保留 XAML 事件处理、共享窗口状态、服务/控制器组装、操作队列和页面间协调；较大的 UI 功能按领域放在 `UI/` 中，独立视图和流程组件各有专职类型。

## 主要入口与协作

- [`MainWindow.xaml.cs`](../../../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs)：初始化配置、服务及控制器，接收窗口生命周期事件。
- `UI/` 下 partial：按账号、内容、下载、实例、启动、整合包、设置、壳层和诊断区域扩展窗口。
- `Controllers/`、`Presentation/` 与 `Builders/`：由窗口初始化或 UI partial 调用，承担具体流程、视图及控件构造；首页版本内容管理、启动选项和整合包导出对话框、离线皮肤打包、启动脚本格式化、游戏窗口行为及通用功能对话框也已移至专职组件。
- 长时间任务通过 `Run` 和现有下载队列执行，以保留互斥、取消和状态更新行为。

## 设计边界

所有 partial 仍是同一个共享状态的 `MainWindow` 类，并非独立后端服务。新增功能应优先采用显式参数与现有组件，不应将服务业务直接写入 XAML 事件回调。

## 源码

- [`MainWindow.xaml.cs`](../../../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs)
- [窗口功能 partial 所在目录](../../../src/MCUGLauncher/MainWindow/UI/)
