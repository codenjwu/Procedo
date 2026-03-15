---
id: runtime-statuses
title: 运行时状态
sidebar_label: 运行时状态
description: 了解 Procedo 在执行、等待、恢复和失败处理期间使用的运行级和步骤级状态值。
---

# 运行时状态

Procedo 会在两个层级上跟踪状态：

- 整个工作流运行
- 运行中的每个单独步骤

这些值会出现在持久化运行时模型、CLI 输出以及执行期间发出的结构化事件中。如果你要运维持久化运行、调查失败，或构建自己的嵌入层，这些状态值都应该被当成标准定义来理解。

## 运行级状态值

`RunStatus` is defined in `src/Procedo.Core/Runtime/RunStatus.cs`.

| 状态 | 含义 | 典型转换 |
| --- | --- | --- |
| `Pending` | 运行已创建，但执行尚未开始。 | 调度开始前的初始状态 |
| `Running` | Procedo 正在调度或执行步骤。 | 引擎开始工作后进入 |
| `Waiting` | 工作流因外部条件、信号或基于文件的恢复条件而暂停。 | 由 `system.wait_signal` 等等待型步骤返回 |
| `Completed` | 所有必需工作都已成功完成。 | 最终成功状态 |
| `Failed` | 运行因步骤失败、依赖被阻塞或运行时致命错误而停止。 | 最终失败状态 |
| `Cancelled` | 执行被运行时或宿主主动取消。 | 最终取消状态 |

## 步骤级状态值

`StepRunStatus` is defined in `src/Procedo.Core/Runtime/StepRunStatus.cs`.

| 状态 | 含义 | 典型转换 |
| --- | --- | --- |
| `Pending` | 步骤已存在于执行图中，但尚未开始。 | 初始状态 |
| `Running` | 步骤正在执行。 | 引擎分派步骤后进入 |
| `Waiting` | 步骤产生了等待描述符，运行进入暂停。 | 常见于 `system.wait_signal`、`system.wait_until` 和 `system.wait_file` |
| `Skipped` | 因 `condition:` 求值为 false，或之前的条件路径使其不再需要，该步骤未执行。 | 最终未执行状态 |
| `Completed` | 步骤成功执行，并且可能产生了输出。 | 最终成功状态 |
| `Failed` | 步骤直接失败，或返回了失败结果。 | 最终失败状态 |

## 状态与等待型工作流的关系

当一个支持等待的步骤暂停执行时：

- 该步骤通常会进入 `Waiting`
- 工作流运行会进入 `Waiting`
- 持久化状态会记录一个 `WaitDescriptor`
- CLI 宿主会返回退出码 `2`，告诉运维人员这是“暂停”而不是“失败”

你可以在下面这个已验证工作流中看到这种行为：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

## 状态与被跳过步骤的关系

`Skipped` 是正常结果，不是错误。它通常意味着：

- 该步骤的 `condition:` 求值为 false
- 模板展开生成了这个步骤，但运行时没有选中对应分支

如果你正在检查一次运行，被跳过的步骤通常应该从工作流定义里解释，而不应该直接当成运行时事故。

## 运维建议

可以这样理解这些状态：

- `Completed`: safe to treat as successful run completion
- `Waiting`: the workflow needs outside action or time to continue
- `Failed`: inspect the failing step, error code, and source path
- `Skipped`: expected non-execution, usually caused by conditions

## 相关内容

- [Persistence](../run-and-operate/persistence)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume)
- [Runtime Persistence State](./runtime-persistence-state)
- [Runtime Error Codes](./runtime-error-codes)
