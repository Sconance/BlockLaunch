# InstanceManagerCommands

内容管理可用的操作集合。CreateMods 接收当前实例键和页面宿主，从组合层创建唯一模组管理页面；视图不直接依赖整个工作区。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Action<Panel, InlineDialog, Func<string>> AttachExtensions`、`Func<bool> CanBrowse`、`Action<RoutedEventArgs> ImportPack`、`Func<bool> IsBusy`、`Action<string> OpenFolder`、`Action<string> OpenInstanceLibrary`、`Action OpenServers`、`Action<string, string> OpenVersionSection`。
- **源码：** [`SketchContentManagerDialog.cs`](../../../src/MCUGLauncher/Presentation/SketchContentManagerDialog.cs)。
