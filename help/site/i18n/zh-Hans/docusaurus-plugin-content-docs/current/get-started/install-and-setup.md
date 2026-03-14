---
title: 安装与环境准备
description: 准备本地环境，以便运行 Procedo 示例。
sidebar_position: 1
---

使用本页把你的机器准备好，以便跑通第一个 Procedo 工作流。

## 前置条件

- 本地已安装 .NET SDK
- 已克隆 Procedo 仓库
- 终端当前位于仓库根目录

## SDK 要求

当前仓库通过 `global.json` 固定了 .NET SDK 版本：

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

这意味着，最稳妥的做法是在本机安装一个兼容该要求的 .NET 10 SDK。

## 框架覆盖范围

当前共享构建配置中的库项目覆盖这些目标框架：

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

## 检查 SDK

```powershell
dotnet --info
```

## 构建解决方案

```powershell
dotnet build Procedo.sln
```

## 构建成功后你可以做什么

构建完成后，你应该可以：

- 使用 `src/Procedo.Runtime` 运行 YAML 工作流
- 运行 `examples/` 下的示例项目
- 验证帮助站点中的代码片段是否与当前仓库状态一致

## 建议先运行的命令

解决方案构建成功后，先执行：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

## 下一步建议

- [Run Your First Workflow](./run-your-first-workflow.md)
- [Procedo CLI Basics](./procedo-cli-basics.md)
