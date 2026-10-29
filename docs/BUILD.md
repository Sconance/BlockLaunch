# 构建与目录说明

## 环境

- Windows x64，安装 `global.json` 指定的 .NET SDK（8.0.426，同 feature band 补丁滚动），不是只安装 Runtime。
- 首次 NuGet 还原需要网络。WPF 界面运行及真实验收必须在 Windows 上完成。
- 游戏需要匹配的 Java；微软内嵌授权需要 WebView2 Runtime。

## 双前端构建

在仓库根目录打开 PowerShell：

```powershell
.\scripts\build-windows.ps1 -Frontend Legacy
.\scripts\build-windows.ps1 -Frontend Front
.\scripts\build-windows.ps1 -Frontend Both
.\scripts\build-windows.ps1 -Frontend Both -Publish
```

执行策略阻止时，仅对本次进程运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -Frontend Both
```

| 入口 | Release 构建输出 |
| --- | --- |
| 旧前端 | `artifacts/windows/legacy/Release/build/MCUGLauncher.exe` |
| Front | `artifacts/windows/front/Release/build/MCUGLauncher.Front.exe` |

发布输出的末级目录为 `publish/`。分发 Front 时保留完整发布目录，不要只复制 EXE。
原 `scripts/build.ps1 -Publish` 继续生成旧入口的完整单文件版与 Lite 版；Lite 需要 .NET 8 Desktop Runtime x64。

## 目录与依赖

| 路径 | 职责 |
| --- | --- |
| `src/MCUGLauncher/MainWindow/`、`Controllers/`、`Presentation/` | 旧 WPF 前端及其交互适配 |
| `src/MCUGLauncher/Front/` | 独立 WPF 新前端，包含入口、视图、ViewModel 和宿主适配 |
| `src/MCUGLauncher/Accounts/`、`Downloads/`、`Settings/`、`Versions/`、`Resources/`、`Launch/`、`Builders/` | 两个前端共用的业务源码，均与 MainWindow 同级 |
| `src/MCUGLauncher/Contracts/`、`Adapters/` | 前端接口及业务适配 |
| `src/MCUGLauncher.Application/` | 编译上述业务源码与接口，不是另一份业务实现 |
| `src/MCUGLauncher.SharedUI/` | 共用 WPF 展示组件，不保存业务策略 |
| `src/MCUGLauncher.Core/` | 底层模型、安装、文件事务、网络与持久化 |
| `scripts/`、`tools/` | 构建、结构检查、预览与源码格式工具 |
| `docs/` | 基本文档、原有参考资料与 Windows 交接 |

两个入口均引用 Application 和 SharedUI，互不引用彼此。Front 保持 C# / .NET 8 / WPF / XAML 技术栈。
测试项目与本轮过程说明已按要求删除，不再提供 `test/` 或 `run-checks.ps1` 命令。

## 工程检查与 Windows 验收

以下脚本是保留的工程检查工具，不是功能测试替代品：

```powershell
python scripts/check-layout.py
python scripts/check-frontend-boundaries.py
python scripts/check-frontend-boundaries.py --build
```

最后一条只在临时副本中分别删除另一套前端后构建，不修改当前源码。
完整操作步骤、临时数据准备与待测清单见 [Windows 交接](dcg/FRONT_UI_WINDOWS_TEST.md)。
Front 默认使用正式配置并恢复待执行下载；未经指南的数据隔离准备，不要直接运行。
本轮增量尚未编译、发布或完成 Windows 验收，保留旧 UI。
