# OfflineSkinPackBuilder

根据本地玩家皮肤和游戏版本元数据生成离线皮肤资源包；在线启动或未配置皮肤时不执行文件操作。

- **主要入口：** `Prepare(root, directory, skinFile, baseVersion, isOffline)`。
- **协作：** 由游戏启动流程调用，读取基础版本 JAR 的资源包格式并将本地皮肤复制到 Steve、Alex 及玩家模型纹理路径。
- **源码：** [`OfflineSkinPackBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/OfflineSkinPackBuilder.cs)。
- **检查：** `VersionUiChecks` 验证离线门控、版本格式元数据、纹理集合及已有资源包替换。
