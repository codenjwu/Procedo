---
title: YAML `condition`
description: 为单个 Procedo 步骤定义运行时执行控制规则。
sidebar_position: 7
---

使用 `condition` 决定某个步骤在运行时是否应该执行。

## 示例

```yaml
condition: eq(params.environment, 'prod')
```

## `condition` 会做什么

- 它会在运行时求值
- 它必须求值为布尔值
- 如果结果为 `false`，该步骤就会被跳过

## 条件中常见的数据来源

- `params.<name>`
- `vars.<name>`
- `steps.<stepId>.outputs.<name>`

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=dev
```

## 相关内容

- [Conditions](../author-workflows/conditions.md)
- [Expressions Condition Rules](./expressions-condition-rules.md)
