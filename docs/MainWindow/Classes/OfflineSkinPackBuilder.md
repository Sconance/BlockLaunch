# OfflineSkinPackBuilder

根据本地玩家皮肤和游戏版本元数据生成离线皮肤资源包；在线启动或未配置皮肤时不执行文件操作。

- **主要入口：** `Prepare(root, directory, skinFile, baseVersion, isOffline)`。
- **协作：** 由游戏启动流程调用，读取基础版本 JAR 的资源包格式并将本地皮肤复制到 Steve、Alex 及玩家模型纹理路径。
- **源码：** [`OfflineSkinPackBuilder.cs`](../../../src/MCUGLauncher/Builders/OfflineSkinPackBuilder.cs)，已迁入与 MainWindow 同级的共享模块，由新旧前端共用的启动控制器调用。
- **独立检查：** `test/BuilderChecks` 不引用 WPF 或前端项目；详见 [共享模块](../../dcg/BUILDERS_MODULE.md)。
- **检查：** `VersionUiChecks` 验证离线门控、版本格式元数据、纹理集合及已有资源包替换。
