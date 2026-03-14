---
title: YAML 工作流结构概览
description: 查看 Procedo 工作流文档的高层结构。
sidebar_position: 2
---

这一页是 Procedo 工作流 YAML 的紧凑型参考入口页。

## 核心结构

```yaml
name: my_workflow
version: 1

parameters:
  environment: dev

stages:
- stage: main
  jobs:
  - job: run
steps:
    - step: say_hello
      type: system.echo
      with:
        message: "Hello"
```

## 主要部分

- `name`
- `version`
- `parameters`
- `variables`
- `stages`
- `jobs`
- `steps`
- `with`
- `condition`
- `depends_on`

## 当前工作流模型

当前仓库中的工作流模型包含这些顶层概念：

- `Name`
- `Version`
- `Template`
- `MaxParallelism`
- `ContinueOnError`
- parameter definitions and values
- variables
- stages

## 参数定义能力

当前参数定义模型支持这些字段：

- `type`
- `required`
- `default`
- `description`
- `allowed_values`
- `min`
- `max`
- `min_length`
- `max_length`
- `pattern`
- `item_type`
- `required_properties`

## Job 级字段

当前 job 支持：

- `job`
- `max_parallelism`
- `continue_on_error`
- `steps`

## Step 级字段

当前 step 支持：

- `step`
- `type`
- `condition`
- `with`
- `depends_on`
- `timeout_ms`
- `retries`
- `continue_on_error`

## 如何使用这一页

把这一页当作紧凑入口来使用。当你需要更详细了解参数、输出或条件等具体主题时，再继续跳转到对应的专门页面。

## 相关内容

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Parameters](../author-workflows/parameters.md)
