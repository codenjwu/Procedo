---
title: 表达式条件规则
description: 了解 Procedo 在求值运行时条件表达式时所遵循的规则。
sidebar_position: 4
---

Procedo 在 `condition` 中使用表达式来决定某个步骤是否执行。

## 规则 1：条件必须求值为布尔值

当前解析器要求条件表达式最终求值为布尔结果。

如果不是布尔值，求值就会失败。

## 规则 2：条件在运行时求值

条件是在执行过程中检查的，而不是在编写工作流文件时检查。

这意味着条件可以依赖：

- parameters
- variables
- outputs from earlier steps

## 规则 3：`false` 表示跳过，而不是失败

如果条件求值为 `false`，运行时会跳过该步骤。

这是正常行为，不是执行错误。

## 规则 4：依赖关系仍然很重要

如果某个步骤引用了前面步骤产生的值，请使用 `depends_on`，让运行顺序与表达式的数据依赖保持一致。

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=prod
```

## 相关内容

- [YAML `condition`](./yaml-condition.md)
- [Conditional Execution](../recipes/conditional-execution.md)
