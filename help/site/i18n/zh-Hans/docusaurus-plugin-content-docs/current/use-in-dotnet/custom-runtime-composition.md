---
title: 自定义运行时组合
description: 组合一个带更严格验证、事件 sink、重试策略和自定义插件集的宿主。
sidebar_position: 4
---

随着宿主能力增长，你通常会需要比最小 builder 配置更丰富的组合方式。

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.Extensible
```

## 这个示例展示了什么

- 同时添加 system 和 demo 插件
- 启用严格验证
- 启用控制台事件输出
- 自定义执行默认值，例如重试和并行度

## 示例模式

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseStrictValidation()
    .UseConsoleEvents()
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 4;
        execution.DefaultStepRetries = 2;
        execution.RetryInitialBackoffMs = 50;
        execution.RetryMaxBackoffMs = 250;
    })
    .Build();
```

## 什么时候使用这种模式

- 面向生产的嵌入式宿主
- 需要一致执行策略的宿主
- 希望从一开始就集成可观测性的应用

## 相关内容

- [ProcedoHostBuilder](./procedo-host-builder.md)
- [Validation](../run-and-operate/validation.md)
