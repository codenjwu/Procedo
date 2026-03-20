---
title: 嵌入 Procedo
description: 在你自己的 .NET 应用中嵌入 Procedo，直接加载、验证并执行工作流。
sidebar_position: 1
---

当你的应用需要直接执行 YAML 定义的工作流，而不是通过外部调用 CLI 宿主时，就应该考虑嵌入 Procedo。

## 推荐的包组合

大多数嵌入场景需要：

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`，如果你要使用内置 `system.*` 步骤

如果你的应用已经使用 `IServiceCollection`，再加上 `Procedo.Extensions.DependencyInjection`。

## 最小宿主示例

```csharp
var yaml = await File.ReadAllTextAsync("examples/01_hello_echo.yaml");

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteYamlAsync(yaml);
```

## 已验证示例应用

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

## 嵌入能带来什么

- 在应用内部加载 YAML
- 执行前进行验证
- 注册自定义步骤
- 集成可观测性和事件输出
- 在宿主管理的运行时中启用持久化和恢复能力
- 通过活动等待查询和按等待身份恢复来实现回调式恢复

## 已验证嵌入示例

除了最小示例之外，当前仓库还提供了更丰富的宿主侧示例：

```powershell
dotnet run --project examples/Procedo.Example.Basic
dotnet run --project examples/Procedo.Example.CallbackResumeHost
dotnet run --project examples/Procedo.Example.AdvancedObservability
dotnet run --project examples/Procedo.Example.ParityRunner
dotnet run --project examples/Procedo.Example.PolicyHost
dotnet run --project examples/Procedo.Example.CustomResolverStore
```

## 相关内容

- [ProcedoHostBuilder](./procedo-host-builder.md)
- [Callback-Driven Resume](./callback-driven-resume)
- [Dependency Injection Integration](../extend-procedo/dependency-injection-integration.md)
