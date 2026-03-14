---
title: 条件
description: 使用 Procedo 条件表达式在运行时控制步骤是否执行。
sidebar_position: 5
---

使用 `condition:` 来决定某个步骤是否应该执行。

条件是在运行时求值的。当工作流整体结构不变，但某些步骤只应在特定输入或状态下执行时，它就非常有用。

## 最小可运行示例

```yaml
steps:
- step: announce
  type: system.echo
  condition: eq(params.environment, 'prod')
  with:
      message: "Deploying to production"
```

## 试一个可运行示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=dev
```

这个命令已经在当前仓库中验证通过。

## 这个示例里发生了什么

这个示例工作流会：

- 始终输出发布标签
- 当 `environment` 不是 `prod` 时跳过生产部署步骤
- 改为执行非生产路径
- 只在开发环境场景下执行额外的校验步骤

关键运行时输出如下：

```text
Release orders-api-dev
Dry-run deployment for orders-api-dev
Skipping [deploy/main/deploy_prod] because condition 'eq(params.environment, 'prod')' evaluated to false.
Validated development release orders-api-dev
```

## 什么时候使用条件

- 按环境区分部署路径
- 可选的验证步骤
- 仅限生产环境的受保护动作
- 条件化的清理或打包操作

## 条件与模板的区别

当某个步骤仍然属于这个工作流，只是有时需要在运行时跳过时，使用 `condition:`。

如果你希望在运行前就改变生成出来的工作流结构，则应该使用模板。

## 相关内容

- [Conditional Execution](../recipes/conditional-execution.md)
- [YAML Workflow Schema Overview](../reference/yaml-workflow-schema-overview.md)
