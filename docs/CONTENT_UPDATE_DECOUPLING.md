# 内容更新控制器解耦

本轮只处理内容更新流程，不改造账号、Java、加载器或卸载控制器。

## 依赖边界

```text
MainWindow：组装 ResourceManagementService + ContentUpdateController
  │
  ├── UpdatesUI 实现 IContentUpdateInteraction
  │     ├── 原 Run / CurrentToken：任务门禁、取消、错误提示、操作历史
  │     ├── 原目标读取与刷新：选中实例、内容目录、完成日志
  │     └── 原下载队列：目标根目录和实例快照入队
  │
  └── ContentUpdateController
        ├── ResourceManagementService：检查、依赖解析、指纹、回退
        └── IContentUpdateSelection：显示选择、确认预览、关闭
              └── ContentUpdateSelectionView → ContentUpdatePreviewView
```

源码：
- [`ContentUpdateController`](../src/MCUGLauncher/Controllers/ContentUpdateController.cs)
- [`IContentUpdateInteraction / IContentUpdateSelection`](../src/MCUGLauncher/Controllers/IContentUpdateInteraction.cs)
- [`窗口适配 UpdatesUI`](../src/MCUGLauncher/MainWindow/UI/Content/UpdatesUI.cs)
- [`ContentUpdateSelectionView`](../src/MCUGLauncher/Presentation/ContentUpdateSelectionView.cs)

控制器不再接收 MainWindow，不引用 System.Windows、MessageBox 或具体视图。
资源服务直接注入，窗口只提供任务/交互能力，不建立暴露控件和所有窗口字段的通用 IMainWindow。
接口中的 owner 只是传给呈现层的宿主标记，控制器不读取或转换它；用于保留版本设置内打开预览的父子关系。
Presentation 仍可依赖 WPF，不要求每个界面组件都变成无框架对象。

## 保持的行为

1. 检查与依赖解析分别进入原 Run；窗口任务适配器保留忙碌门禁、异常处理与取消令牌，不能另起协调器。
2. 检查/预览失败、取消或任务未执行时不显示下一层确认；取消预览不入队、不关闭选择页。
3. 修改前继续读取当前游戏运行状态；底层仍复用原依赖校验、内容指纹、事务和提交边界。
4. 确认后入队保存的方案与指纹，成功入队后才关闭选择页；更新仍由原 DownloadTaskExecutor 执行。
5. 回退先确认，成功后才刷新列表并记录日志；用户修改过的文件仍由事务实现拒绝回退。

选择页仍通过 isBusy 回调阻止任务执行期间关闭。预览父页、按钮文字、项目筛选和展示布局保持不变。

## 目标快照

Open 接收 ResourceInstallTarget，明确保存根目录、实例、目标内容目录、Minecraft 版本和加载器。
MainWindow 的原 OpenContentUpdates 签名保留，原页面与界面检查程序不需要改变调用方式。
EnqueueDownload 新增可选 root 参数：内容更新用最初快照的根目录；其他调用仍默认当前 prefs.Root。
这避免用户在检查后切换目录时将旧安装方案登记到新的根目录；实例内容目标也不重新读取控件。

任务历史的 Run 元数据和日志/刷新仍由窗口现有适配处理，本轮不迁移整个任务系统或所有页面生命周期。

## 隔离回归检查

[`ContentUpdateControllerChecks`](../test/CompatibilityChecks/ContentUpdateControllerChecks.cs) 将控制器与接口源码链接到
CompatibilityChecks，不引用 WPF 主项目，使用真实 ResourceManagementService、临时文件和合成 HTTP。
覆盖：确认入队、拒绝预览、目标切换、忙碌、目标读取失败、游戏运行、取消、空选择、损坏记录、
回退确认、真实事务回退以及用户修改后的回退阻止。

```powershell
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --content-update-controller
```

不带参数时 CompatibilityChecks 也执行该检查。现有 WorkflowUiChecks 继续使用原 OpenContentUpdates 入口，
用于 Windows 上验证选择页、预览、确认以及带指纹的队列方案。

当前环境缺少 .NET SDK，未编译、未运行上述检查，也未执行 Windows 界面回归；静态边界和布局检查不能代替这些验证。
