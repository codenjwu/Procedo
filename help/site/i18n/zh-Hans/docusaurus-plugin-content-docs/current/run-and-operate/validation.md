---
title: 验证
description: 在执行进入运行时之前，尽早发现工作流错误。
sidebar_position: 3
---

验证可以帮助你在工作流真正运行之前，就发现结构、依赖关系和参数方面的问题。

## 为什么验证很重要

验证是 Procedo 最实用的能力之一。它能阻止那些明显的工作流问题变成让人困惑的运行时失败。

验证通常可以捕获这些问题：

- 无效的 step type
- 未知依赖
- 依赖环
- 非法参数值

## 示例来源

- `examples/13_missing_plugin_validation_error.yaml`
- `examples/14_cycle_dependency_validation_error.yaml`
- `examples/49_parameter_schema_validation_demo.yaml`

## 合法输入示例

这个命令已经在当前仓库中验证通过：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## 真实的验证失败示例

当前运行这些非法示例时，会得到类似下面的错误：

```text
[ERROR] PV304 ... No plugin registered for step type 'no.such.plugin'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

以及：

```text
[ERROR] PV309 ... Cyclic dependency detected in stage 'validate', job 'cycle'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

## 它的重要性

- 对无效工作流快速失败
- 尽早发现依赖问题
- 验证运行时输入是否符合预期

## 良好实践

把验证当作日常编写流程的一部分，而不是只在工作流已经坏掉之后才去做的补救措施。

## 相关内容

- [Common Validation Errors](../troubleshooting/common-validation-errors.md)
- [Parameters](../author-workflows/parameters.md)
