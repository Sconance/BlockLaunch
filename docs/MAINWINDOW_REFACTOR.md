# MainWindow 职责拆分

## 当前结构

原先 42 个 MainWindow 源文件中的职责已迁移到 41 个独立模块。MainWindow 只保留原生窗口、初始化组合与页内对话框适配；不再存放账号、下载队列、实例、资源查询等业务字段。

- `Composition/LauncherWorkspace.cs`：模块的构造与生命周期组合，不保存业务状态。
- `Composition/LauncherControls.cs`：九个页面视图的命名控件绑定，不通过字符串查找控件。
- `Composition/LauncherPageRouter.cs`：页面动作到具体模块的编译期可检查路由，不使用反射。
- `Views/Pages/`：启动、下载、资源、内容管理、设置、日志、任务、导航和标题区九个 XAML 视图。
- `Components/`：各功能控制器拥有各自字段、事件和操作。启动协调位于 `LaunchCoordinator`，版本设置位于 `VersionSettingsController`，实例管理位于 `InstancePagesController` 和 `SketchController`，资源和任务也各自独立。
- `Views/`、`Presentation/`：第一批独立控件和加载器 Presenter 继续使用，不退回主窗口大类。

## 页面组合与兼容

已有动态布局会移动或重组控件。为了保持视图树和行为，本轮由 `LauncherPage.Materialize` 将独立 XAML 的内容放入原来的宿主，保留页面工厂持有的命名控件与事件，随后使用强类型绑定连接控制器。页面不依赖 MainWindow 的私有字段。

窗口创建页面、组合所有模块，再执行启动初始化。控件初始化早期的动作不会访问尚未创建的工作区；后续页面动作通过专用路由调用模块。原生窗口接口和页内对话框接口保持兼容。

原有认证、启动、下载与实例 Core 实现未重写。账号加密熵、配置目录、JSON 字段和实例存储格式保持原样。独立的公开控件与展示模型仍保留原有公开契约。

## 开发约束

新增页面和栏目沿用 [菜单扩展接口](MENU_EXTENSIONS.md)。页面布局放在 Views，功能状态放在所属 Components 或 Presenter；不要把模块字段重新汇总到窗口或组合根。模块间协作应明确指出所属模块；后续可以继续将跨模块依赖收窄为专用接口，但不要再以 partial MainWindow 共享状态。

此结构不意味着所有模块都已零耦合，也不是另起一套 MVVM 框架。它完成的是窗口职责与页面/功能模块的分离；模块内部的大方法还可以按实际修改需求继续整理。

`scripts/check-ui-boundaries.py` 检查窗口大小边界、独立 XAML 页及生产代码中是否重新引入 MainWindow 功能文件；已接入 CI。

## 验证边界

测试仅在 `test/Support/LauncherTestAccess.cs` 中使用模块检查适配器，原有功能断言继续执行，不在生产窗口保留用于兼容测试的私有字段或业务方法。该适配器仅链接到检查/预览项目。

真实微软/MUA 人工授权及完整游戏启动矩阵未重跑。组件检查、真实资源下载和合成实例检查不能替代这些验收。本轮尚未发布，不覆盖现有 Release 与本地正式 EXE。
