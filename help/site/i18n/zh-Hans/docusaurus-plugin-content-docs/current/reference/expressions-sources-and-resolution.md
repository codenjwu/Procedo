---
title: 表达式来源与解析
description: 了解 Procedo 如何在表达式中解析参数、变量和步骤输出引用。
sidebar_position: 2
---

Procedo 会基于运行时变量映射来解析表达式。

当前示例和解析器行为支持这些常见引用模式：

- `params.<name>`
- `vars.<name>`
- `steps.<stepId>.outputs.<name>`

## 示例

```yaml
message: "${params.environment}"
message: "${vars.release_label}"
message: "${steps.producer.outputs.message}"
```

## 解析行为

当前解析器行为包括：

- 直接变量查找
- 支持带前缀和短键形式的 `vars.` 查找
- 支持带前缀和短键形式的 `params.` 查找
- 通过 `steps.<stepId>.outputs.<name>` 查找步骤输出

## 如果解析失败会发生什么

如果某个 token 无法被解析，表达式解析器会抛出表达式解析错误。

## 相关内容

- [Expression Functions](./expressions-functions.md)
- [Outputs](../author-workflows/outputs.md)
