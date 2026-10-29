# 发布说明

每个版本一份发布说明，手动整理，来源是当次发布的两类事实：

1. **变更内容**：`CHANGELOG.md` 中对应版本的小节（发布说明只做摘要与措辞整理，不新增未记录的变化）；
2. **发布文件**：`scripts/build.ps1 -Publish` 的产物、`FileVersion` / `ProductVersion` 与 SHA-256。

说明：

- 文件按版本命名（如 `0.22.0.md`），与 `v<版本>` 标签、`Directory.Build.props` 的 `McugVersion` 对应；
- 二进制 EXE 不进入版本库，发布时上传到 GitHub Releases；0.22.1 起由 Release 正文及 SHA256SUMS.txt 记录附件哈希，以免文档提交与构建元数据相互依赖；
- 若正式构建与本文记录的提交不一致，以标签提交重新构建的哈希为准，并更新本文件。

当前版本：[0.22.1](0.22.1.md)。上一版本：[0.22.0](0.22.0.md)。
