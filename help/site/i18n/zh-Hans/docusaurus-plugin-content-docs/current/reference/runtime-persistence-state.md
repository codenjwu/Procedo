---
id: runtime-persistence-state
title: 运行时持久化状态
sidebar_label: 持久化状态
description: 了解 Procedo 为持久化运行存储了什么，以及运行状态、步骤状态、输出和等待元数据是如何表示的。
---

# 运行时持久化状态

当你在启用持久化的情况下运行 Procedo 时，运行时会把序列化后的运行模型写入磁盘。这个模型是恢复等待型工作流、检查历史运行以及判断哪些步骤已经完成的事实来源。

主要持久化类型位于：

- `src/Procedo.Core/Runtime/WorkflowRunState.cs`
- `src/Procedo.Core/Runtime/StepRunState.cs`
- `src/Procedo.Core/Runtime/WaitDescriptor.cs`

## 工作流级字段

持久化的 `WorkflowRunState` 包含这些关键字段：

| 字段 | 含义 |
| --- | --- |
| `PersistenceSchemaVersion` | 持久化文件结构的版本标记 |
| `RunId` | 用于检查和恢复的稳定标识 |
| `WorkflowName` | 来自 YAML 的工作流 `name` |
| `WorkflowVersion` | 来自 YAML 的工作流 `version` |
| `Status` | 当前运行状态，例如 `Running`、`Waiting` 或 `Completed` |
| `Error` | 运行失败时的顶层错误文本 |
| `CreatedAtUtc` | 运行记录创建时间 |
| `UpdatedAtUtc` | 运行记录最后更新时间 |
| `WaitingStepKey` | 如果运行处于暂停状态，表示当前等待步骤的标识 |
| `WaitingSinceUtc` | 进入等待状态的时间戳 |
| `Steps` | 以步骤标识为键的每步运行时状态 |

## 步骤级字段

`WorkflowRunState.Steps` 中的每个步骤条目都是一个 `StepRunState`，包含：

| 字段 | 含义 |
| --- | --- |
| `Stage` | 所属 stage 名称 |
| `Job` | 所属 job 名称 |
| `StepId` | 来自 YAML 的步骤标识 |
| `Status` | 当前步骤状态 |
| `Error` | 步骤级失败文本 |
| `StartedAtUtc` | 执行开始时间 |
| `CompletedAtUtc` | 执行结束时间 |
| `Outputs` | 为下游表达式解析和恢复执行保留的输出值 |
| `Wait` | 步骤暂停工作流时的 `WaitDescriptor` 数据 |

## 等待描述符字段

当某个步骤暂停执行时，Procedo 会持久化一个 `WaitDescriptor`：

| 字段 | 含义 |
| --- | --- |
| `Type` | 等待机制，例如信号、文件或时间等待 |
| `Reason` | 人类可读的等待原因（如果有） |
| `Key` | 如果该等待类型使用恢复关联键，则这里保存它 |
| `Metadata` | 运行时或运维侧需要的额外细节 |

正是这个模型让本地恢复成为可能，而且不需要重新执行已经完成的步骤。

## 持久化是为了解决什么问题

持久化主要用于：

- local host recovery
- operator inspection
- pause/resume workflows
- output rehydration for downstream steps after resume

它不是一个集群化、分布式编排状态存储。当前支持模型仍然是单节点、基于文件的实现。

## 运维示例流程

启动一个持久化运行：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/help-docs-runs
```

让运行进入等待状态：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

在这两种情况下，Procedo 都会存储足够的信息，用于识别该运行、检查步骤结果，并在之后从持久化状态继续执行。

## 嵌入式使用者应该如何理解

如果你围绕持久化状态构建自己的工具：

- 把运行时模型视为运维数据，而不是长期公共存储契约
- 依赖文档化的状态含义和错误码，而不是字段顺序或文件格式这类偶然细节
- 把 `Outputs` 视为恢复执行时下游表达式求值的标准数据来源

## 相关内容

- [Persistence](../run-and-operate/persistence)
- [Runtime Statuses](./runtime-statuses)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume)
- [Known Limitations](../whats-new/known-limitations)
