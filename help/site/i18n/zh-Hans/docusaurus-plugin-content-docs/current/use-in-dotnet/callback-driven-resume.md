---
title: 回调式恢复
description: 在 .NET 宿主中查询活动等待，并按等待身份恢复持久化运行。
sidebar_position: 4
---

回调式恢复是宿主侧的一种恢复模式：当你还不知道 `runId`，但收到了审批、回调或其他外部信号时，先定位正确的等待运行，再恢复它。

这类能力特别适合：

- 人工审批
- 外部系统回调
- 宿主进程中的等待匹配与恢复路由

## 需要什么

宿主需要具备：

- 已配置的运行状态存储
- 工作流定义解析器
- 目标工作流所需的插件注册

对于本地文件持久化，`UseLocalRunStateStore(...)` 会同时配置内置文件存储和默认文件型工作流解析器。

## 核心宿主 API

主要 API 是：

- `FindWaitingRunsAsync(...)`
- `ResumeWaitingRunAsync(...)`

对应的公开模型包括：

- `WaitingRunQuery`
- `ActiveWaitState`
- `ResumeWaitingRunRequest`
- `WaitingRunMatchBehavior`

## 最小示例

```csharp
using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs")
    .Build();

var waiting = await host.FindWaitingRunsAsync(new WaitingRunQuery
{
    WaitType = "signal",
    WaitKey = "callback-identity-demo",
    ExpectedSignalType = "approve"
});

var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
{
    WaitType = "signal",
    WaitKey = "callback-identity-demo",
    ExpectedSignalType = "approve",
    SignalType = "approve",
    Payload = new Dictionary<string, object>
    {
        ["approved_by"] = "ops-bot",
        ["ticket"] = "CHG-710"
    }
});
```

## 重复匹配行为

当多个等待运行都可能匹配时，`ResumeWaitingRunAsync(...)` 会通过 `WaitingRunMatchBehavior` 明确决定行为：

- `FailWhenMultiple`
- `ResumeNewest`
- `ResumeOldest`

默认最安全的选择是 `FailWhenMultiple`。

## 活动等待查询会返回什么

`ActiveWaitState` 会把当前等待状态投影成适合宿主使用的模型，包括：

- workflow name
- run id
- stage / job / step 标识
- wait type 和 wait key
- expected signal type
- waiting timestamp
- 可选等待元数据

这样宿主应用就不需要直接读取原始持久化 JSON，就能完成等待匹配和路由。

## 工作流快照安全性

对于回调式恢复，Procedo 会把进入等待状态时的工作流快照一起持久化。

这样做的目的是确保恢复时使用的是“当时进入等待”的工作流定义，而不是后来被修改过的 YAML 文件。

如果某个等待运行早于工作流快照机制，那么自动回调式恢复会受到限制。

## 当前范围

Phase 1 的回调式恢复已经适用于单机、本地文件持久化模型。

它不是一个分布式锁，也不是多节点协调能力。

## 已验证示例项目

最适合查看真实代码模式的示例项目是：

```powershell
dotnet run --project examples/Procedo.Example.CallbackResumeHost
dotnet run --project examples/Procedo.Example.CustomResolverStore
```

## 相关内容

- [Embedding Procedo](./embedding-procedo)
- [ProcedoHostBuilder](./procedo-host-builder)
- [Persistence](../run-and-operate/persistence)
- [Built-in Steps: Wait and Resume](../reference/built-in-steps-wait-and-resume)
