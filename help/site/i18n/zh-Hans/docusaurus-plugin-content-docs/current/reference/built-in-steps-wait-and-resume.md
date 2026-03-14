---
title: "内置步骤：等待与恢复"
description: 使用内置等待步骤构建审批式和信号驱动的工作流流程。
sidebar_position: 24
---

Procedo 为需要暂停并在稍后继续的工作流提供了内置步骤。

当前面向等待场景的 step type 包括：

- `system.wait_signal`
- `system.wait_until`
- `system.wait_file`

## 等待信号

最简单的等待示例如下：

```yaml
- step: wait_here
  type: system.wait_signal
  with:
    signal_type: continue
    reason: "Waiting for external continue signal"
```

## 已验证等待示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

## 你可以期待什么

当前运行时行为会体现出：

- 工作流进入等待状态
- 运行时输出等待原因
- 输出 run id
- 输出状态目录

这个命令经过验证后的 shell 退出码是 `2`，这也是运行时用来表示“结果为等待状态”的方式。

## 什么时候使用等待步骤

- 审批型工作流
- 外部协调点
- 基于文件的就绪检查
- 由运维人员驱动的继续执行流程

## 相关内容

- [Persistence](../run-and-operate/persistence.md)
- [CLI Overview](./cli-overview.md)
