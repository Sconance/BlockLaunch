# InstanceSearchController

按用户输入过滤实例列表，并更新匹配数量摘要；搜索词可匹配实例名称、键及版本/加载器详情。

- **主要入口：** `Apply()`，由查询文本变化和实例列表刷新触发。
- **协作：** 接收列表、搜索框和摘要控件；识别 [InstanceView](InstanceView.md) 项。
- **源码：** [`InstanceSearchController.cs`](../../../src/MCUGLauncher/Controllers/InstanceSearchController.cs)。
