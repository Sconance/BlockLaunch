# ContentSearchFilterBuilder

构造资源搜索页的关键词、游戏版本、加载器、来源和发布频道筛选控件及重置操作。

- **职责边界：** 只组合控件和筛选状态，不执行网络搜索；搜索由内容页面/服务处理。
- **协作：** 由 `MainWindow` 的资源筛选初始化流程使用。
- **源码：** [`ContentSearchFilterBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/ContentSearchFilterBuilder.cs)。
