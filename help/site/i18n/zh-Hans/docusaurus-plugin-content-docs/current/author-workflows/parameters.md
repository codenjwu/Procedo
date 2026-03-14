---
title: 参数
description: 向 Procedo 工作流传入运行时输入，并验证这些值是否符合预期。
sidebar_position: 3
---

参数让工作流能够在运行时接收输入值，而不是把每个决定都写死在 YAML 里。

它是让同一个工作流在不同环境、服务和执行上下文中复用的主要方式。

## 最小可运行示例

```yaml
parameters:
  environment: prod
```

运行时覆盖参数：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## 更完整的参数示例

Procedo 也支持更丰富的参数定义。下面这个仓库示例已经通过文档中的命令验证成功：

```yaml
parameters:
  service_name:
    type: string
    min_length: 3
    max_length: 20
    pattern: "^[a-z][a-z0-9-]+$"
    default: procedo-api

  environment:
    type: string
    allowed_values:
    - dev
    - prod
    default: dev

  retry_count:
    type: int
    min: 1
    max: 5
    default: 2
```

用合法输入运行它：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## 什么时候使用参数

- 环境选择
- 区域或租户选择
- 功能开关
- 用户提供的运行时值

## 参数带来的价值

- 把工作流结构和运行时值更清晰地分离
- 减少重复的工作流文件
- 在执行入口处完成验证
- 更好地支持模板和发布推进类工作流

## 常见建议

- 如果工作流存在常见默认路径，就提供简单默认值
- 当错误输入会导致不安全或误导性行为时，添加约束
- 如果只是运行时值不同，优先使用参数，而不是为每个环境改一份 YAML

## 相关内容

- [Validation](../run-and-operate/validation.md)
- [YAML Workflow Schema Overview](../reference/yaml-workflow-schema-overview.md)
