# InstanceView

实例列表的展示模型。VersionSummary 只输出游戏版本与加载器信息；缺少可识别版本元数据时显示“版本未知”，避免重复自定义实例名。

- **类型：** `public record`，基类/实现：无。
- **构造依赖：** `string Detail`、`string Group`、`string Icon`、`int JavaMajor`、`string Key`、`string Name`。
- **源码：** [`InstanceView.cs`](../../../src/MCUGLauncher/Models/InstanceView.cs)。
