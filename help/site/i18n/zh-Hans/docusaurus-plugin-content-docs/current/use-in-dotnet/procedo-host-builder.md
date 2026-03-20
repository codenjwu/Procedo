---
title: ProcedoHostBuilder
description: 使用 ProcedoHostBuilder 配置插件、执行策略、验证、日志和持久化。
sidebar_position: 2
---

`ProcedoHostBuilder` 是嵌入 Procedo 时最主要的高层组合入口。

## 当前 Builder 提供的能力

当前 builder 包含这些方法：

- `Configure(...)`
- `ConfigurePlugins(...)`
- `UseServiceProvider(...)`
- `ConfigureExecution(...)`
- `ConfigureValidation(...)`
- `UseLogger(...)`
- `UseEventSink(...)`
- `UseRunStateStore(...)`
- `UseLocalRunStateStore(...)`
- `UseWorkflowDefinitionResolver(...)`
- `Build()`

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.Extensible
```

## 为什么它重要

这个 builder 是集中配置下列内容的最简单方式：

- 插件注册
- 执行策略默认值
- 验证行为
- 日志器选择
- 事件 sink 选择
- 持久化配置
- 回调式恢复所需的工作流定义解析

## 相关内容

- [Embedding Procedo](./embedding-procedo.md)
- [Callback-Driven Resume](./callback-driven-resume)
- [Custom Runtime Composition](./custom-runtime-composition.md)
