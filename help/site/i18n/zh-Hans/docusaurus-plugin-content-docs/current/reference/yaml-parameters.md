---
title: YAML `parameters`
description: 为 Procedo 工作流定义运行时输入及其约束。
sidebar_position: 4
---

使用 `parameters` 来声明调用方可以在运行时提供或覆盖的工作流输入。

## 简单形式

```yaml
parameters:
  environment: dev
```

当你只需要默认值、不需要额外约束时，这种形式很适合。

## 丰富定义形式

当前仓库示例中使用了更丰富的参数定义，例如：

```yaml
parameters:
  service_name:
    type: string
    min_length: 3
    max_length: 20
    pattern: "^[a-z][a-z0-9-]+$"
    default: procedo-api
```

## 支持的定义字段

当前 `ParameterDefinition` 模型支持：

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

## 什么时候使用丰富定义

在这些场景下，建议使用更丰富的参数定义：

- 非法输入应该尽早失败
- 用户需要更明确的允许值范围
- 工作流会在不同环境或团队之间共享
- 模板依赖稳定可靠的参数形状

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## 相关内容

- [Parameters](../author-workflows/parameters.md)
- [Validation](../run-and-operate/validation.md)
