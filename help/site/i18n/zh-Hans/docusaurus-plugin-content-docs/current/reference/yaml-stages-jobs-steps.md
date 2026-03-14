---
title: YAML `stages`、`jobs` 与 `steps`
description: 了解 Procedo 工作流使用的核心分层执行结构。
sidebar_position: 5
---

Procedo 把可执行工作流内容组织成 `stages`、`jobs` 和 `steps` 三层。

## 示例

```yaml
stages:
- stage: build
  jobs:
  - job: pipeline
    steps:
    - step: download
      type: system.echo
      with:
        message: "download"
```

## `stages`

每个 stage 表示一个较大的工作阶段。

当前模型字段：

- `stage`
- `jobs`

## `jobs`

每个 job 用来组织相关步骤，并且可以携带执行策略设置。

当前模型字段包括：

- `job`
- `max_parallelism`
- `continue_on_error`
- `steps`

## `steps`

每个 step 都是一个单独的可执行单元。

当前 step 字段包括：

- `step`
- `type`
- `condition`
- `with`
- `depends_on`
- `timeout_ms`
- `retries`
- `continue_on_error`

## 建模建议

- 用 stage 表示较大阶段
- 用 job 表示运维或逻辑分组
- 用 step 表示最小且清晰的工作单元

## 相关内容

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Steps](../author-workflows/steps.md)
