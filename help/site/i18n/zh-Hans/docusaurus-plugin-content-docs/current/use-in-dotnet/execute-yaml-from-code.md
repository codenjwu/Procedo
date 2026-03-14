---
title: 在代码中执行 YAML
description: 从你的 .NET 应用中加载工作流文件或 YAML 字符串并直接执行。
sidebar_position: 3
---

最简单的嵌入模式之一，就是在进程内加载 YAML 并直接执行。

## 底层的解析、验证、执行流程

已验证的基础示例大致采用下面这种结构：

```csharp
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
var workflow = new YamlWorkflowParser().Parse(yaml);

IPluginRegistry registry = new PluginRegistry();
registry.AddSystemPlugin();
registry.AddDemoPlugin();

var validation = new ProcedoWorkflowValidator().Validate(workflow, registry);
var engine = new ProcedoWorkflowEngine();
var result = await engine.ExecuteAsync(workflow, registry, new ConsoleLogger()).ConfigureAwait(false);
```

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

## 什么时候使用这种模式

- 你希望显式控制解析、验证、执行三个阶段
- 你需要在工作流加载前后做自定义预处理
- 你的应用希望获得更底层的运行时控制能力

## 相关内容

- [Embedding Procedo](./embedding-procedo.md)
- [ProcedoHostBuilder](./procedo-host-builder.md)
