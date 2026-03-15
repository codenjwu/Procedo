---
id: runtime-error-codes
title: 运行时错误码
sidebar_label: 错误码
description: Procedo 运行时错误码参考，包括它们的含义以及在验证、执行、等待和恢复流程中的解释方式。
---

# 运行时错误码

Procedo 使用简短的 `PRxxx` 错误码来表示常见的运行时和宿主级结果。这些常量定义在 `src/Procedo.Core/Models/RuntimeErrorCodes.cs` 中。

在错误处理、仪表盘和排障场景中，应该把这些错误码当作稳定标识。错误文本的措辞以后可能变化，但错误码更适合做自动化处理。

## 错误码表

| 代码 | 名称 | 含义 |
| --- | --- | --- |
| `PR000` | `None` | No runtime error was recorded |
| `PR100` | `JobFailed` | A job failed because one of its steps or dependencies did not complete successfully |
| `PR101` | `PluginNotFound` | A referenced step type could not be resolved from the registered plugins |
| `PR102` | `StepResultFailed` | A step returned a failed result explicitly |
| `PR103` | `StepException` | A step threw an exception during execution |
| `PR104` | `StepTimeout` | A step exceeded its allowed runtime or timeout policy |
| `PR105` | `Cancelled` | The run was cancelled before successful completion |
| `PR106` | `DependencyBlocked` | A step or job could not run because an upstream dependency failed or was blocked |
| `PR107` | `SchedulerDeadlock` | The engine detected a graph progress problem and could not continue scheduling |
| `PR108` | `Waiting` | The run is paused in a waiting state rather than finished |
| `PR109` | `InvalidResume` | A resume request was invalid for the current persisted run state |
| `PR200` | `WorkflowLoadFailed` | The workflow file or source could not be loaded correctly |
| `PR201` | `ValidationFailed` | Validation failed before execution could begin |
| `PR202` | `ConfigurationInvalid` | Runtime configuration was invalid |
| `PR203` | `WorkflowFileNotFound` | The supplied workflow file path did not exist |

## 实际中最常见的错误码

运维人员最常遇到的通常是：

- `PR201`: invalid YAML shape, dependency errors, unknown step types, or invalid parameter input
- `PR101`: plugin registration mismatch
- `PR108`: waiting workflows such as `system.wait_signal`
- `PR109`: trying to resume the wrong run or using resume data that does not match the waiting state

## 验证阶段与执行阶段

把错误码分成两类会更容易理解：

验证与准备阶段：

- `PR200`
- `PR201`
- `PR202`
- `PR203`

执行与运行时阶段：

- `PR100`
- `PR101`
- `PR102`
- `PR103`
- `PR104`
- `PR105`
- `PR106`
- `PR107`
- `PR108`
- `PR109`

## 等待不等于失败

`PR108` 表示工作流是“有意暂停”的。在 CLI 宿主中，这个状态还会体现在退出码 `2` 上，这样自动化逻辑就可以区分：

- 成功
- 失败
- 已暂停且可恢复

这种行为已经通过文档代码片段套件中的 `examples/45_wait_signal_demo.yaml` 得到验证。

## 排障建议

当你在自己的工具中展示错误时：

- 同时展示错误码和错误消息
- 给错误码关联到用户可读的排障文档
- 如果有的话，一并展示 step id、source path 和等待元数据

## 相关内容

- [Common Validation Errors](../troubleshooting/common-validation-errors)
- [Runtime Statuses](./runtime-statuses)
- [CLI Overview](./cli-overview)
- [Validation](../run-and-operate/validation)
