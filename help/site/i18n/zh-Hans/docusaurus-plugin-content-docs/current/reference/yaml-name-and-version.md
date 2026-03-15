---
title: YAML `name` 与 `version`
description: 了解每个 Procedo 工作流都会使用的两个顶层标识字段。
sidebar_position: 3
---

每个 Procedo 工作流都应该声明 `name` 和 `version`。

## 示例

```yaml
name: hello_echo
version: 1
```

## `name`

`name` 用于在日志、运行时输出以及持久化运行状态中标识工作流。

一个好的名字通常应该：

- 稳定
- 可读
- 能体现工作流用途

## `version`

`version` 表示工作流文档版本。

当前仓库中的示例统一使用：

```yaml
version: 1
```

## 实际建议

- 对同一个工作流，尽量保持 `name` 一致
- 当你需要明确体现文档演进时，递增或跟踪 `version`
- 在共享工作流中，避免使用 `test`、`workflow1` 这类含糊名称

## 相关内容

- [YAML Workflow Schema Overview](./yaml-workflow-schema-overview.md)
- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
