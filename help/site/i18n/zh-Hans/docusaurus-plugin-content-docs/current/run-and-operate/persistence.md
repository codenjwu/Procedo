---
title: 持久化
description: 持久化工作流运行状态，使等待中的流程或中断后的流程可以稍后继续执行。
sidebar_position: 1
---

持久化让 Procedo 可以把运行状态存储在当前进程之外。

对于下面这些工作流，持久化尤其重要：

- 需要等待人工审批
- 需要稍后恢复执行
- 需要保留可检查的运行状态
- 需要超出单个进程生命周期的运维恢复能力

## 最小可运行示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/runs
```

当前仓库已经用同样的命令结构在临时状态目录上完成了验证。

## 为什么要使用持久化

- 之后恢复工作流
- 检查已保存的状态
- 支持等待后继续的执行模式

## 你可以期待什么

一次成功的持久化运行完成后，运行时会打印 run id，以及存储运行记录的状态目录。

例如：

```text
[INFO] Workflow 'persistence_resume_happy_path' completed successfully.
[INFO] Run id: <runId>
[INFO] Run state directory: <state directory path>
```

## 什么时候应该尽早用它

如果你的工作流更像运维流程，而不是一次性短脚本，就应该尽早使用持久化。

当你引入下面这些能力之后，持久化会变得尤其重要：

- 等待步骤
- 恢复信号
- 审批检查点
- 外部对运行状态的检查

## 相关内容

- [Observability](./observability.md)
- [CLI Overview](../reference/cli-overview.md)
