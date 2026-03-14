---
title: 运行你的第一个工作流
description: 通过 CLI 宿主运行最小可用的 Procedo 工作流。
sidebar_position: 2
---

理解 Procedo 最快的方法，就是端到端跑一个很小的工作流，然后观察运行时输出了什么。

本教程使用仓库中最小、并且能在内置 system 插件支持下成功运行的示例。

## 最小可运行示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

这个命令已经在当前仓库中验证过，可以成功完成执行。

## 工作流源码

```yaml
name: hello_echo
version: 1
stages:
- stage: demo
  jobs:
  - job: simple
    steps:
    - step: hello
      type: system.echo
      with:
        message: "Hello Procedo"
```

## 这个命令会做什么

这个命令会让参考 CLI 宿主执行一个简单的 YAML 工作流，其中只有一个 `system.echo` 步骤。

运行时会：

- 加载 YAML 定义
- 验证工作流结构
- 构建执行图
- 运行 `system.echo` 步骤
- 打印运行摘要

## 预期输出

在这个仓库中运行时，关键输出大致如下：

```text
[INFO] Starting workflow 'hello_echo'
[INFO] Stage: demo
[INFO] Job: simple
[INFO] Running [demo/simple/hello] (system.echo) attempt 1/1
Hello Procedo
[INFO] Workflow 'hello_echo' completed successfully.
```

每次运行的 run id 都会变化，所以你机器上的那一行会和这里不同，这是正常的。

## 为什么这个示例很重要

这个示例虽然很小，但已经包含了 Procedo 工作流的最小完整结构：

- one workflow
- one stage
- one job
- one step
- one built-in step type

只要这个例子跑通，就说明你的本地环境已经具备执行 Procedo 核心运行路径的能力。

## 运行时值得注意的点

- 工作流名称会出现在开始和完成日志中。
- 运行时进入 stage 和 job 时会打印对应名称。
- 执行日志中会显示 step id 和 step type。
- `system.echo` 会直接把配置的消息输出出来。

## 下一步

- [Create Your First Workflow](./create-your-first-workflow.md)
- [Minimal Pipeline](../recipes/minimal-pipeline.md)
