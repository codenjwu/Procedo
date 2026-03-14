---
title: 依赖注入集成
description: 使用 Procedo 的 DI 包，通过 IServiceCollection 注册宿主和步骤。
sidebar_position: 4
---

如果你的应用已经使用 `Microsoft.Extensions.DependencyInjection`，那么可以通过 `Procedo.Extensions.DependencyInjection` 包，把 Procedo 集成到 `IServiceCollection` 中。

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.DependencyInjection
```

## 这个示例展示了什么

- `services.AddProcedo()`
- system 插件注册
- 基于 DI 的自定义步骤注册
- 委托步骤注册
- 方法绑定注册
- 从 service provider 解析一个可直接使用的 `ProcedoHost`

## 示例模式

```csharp
services.AddProcedo()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .RegisterStep<DiGreetingStep>("custom.di_greeting")
    .RegisterStep("custom.delegate_suffix", static context => new StepResult { ... })
    .RegisterMethod("custom.compose_message", (Func<string, string, ComposedMessage>)ComposeMessage);
```

## 相关内容

- [Embedding Procedo](../use-in-dotnet/embedding-procedo.md)
- [Create a Custom Step](./create-a-custom-step.md)
