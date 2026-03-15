---
title: YAML `with` 与 `depends_on`
description: 传递步骤输入，并表达步骤之间的执行依赖关系。
sidebar_position: 6
---

`with` 和 `depends_on` 是 step 级别最重要的两个工作流字段之一。

## `with`

使用 `with` 给某个 step type 传入输入值。

示例：

```yaml
with:
  message: "Hello Procedo"
```

`with` 下面支持哪些具体字段，取决于 step type 本身。

## `depends_on`

使用 `depends_on` 表示某个步骤必须等待一个或多个其他步骤完成。

示例：

```yaml
depends_on:
- producer
```

## 组合示例

```yaml
- step: consumer
  type: system.echo
  depends_on:
  - producer
  with:
    message: "from producer: ${steps.producer.outputs.message}"
```

## 什么时候一起使用它们

这种组合常见于：

- 某个步骤要消费另一个步骤的输出
- 执行必须严格按顺序进行
- 某个汇聚步骤需要等待多个前置条件

## 相关内容

- [Outputs](../author-workflows/outputs.md)
- [Dependencies and Execution Order](../recipes/dependencies-and-execution-order.md)
