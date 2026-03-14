---
title: 模板参数
description: 定义模板的参数化输入，并在子工作流或运行时调用方中覆盖它们。
sidebar_position: 2
---

模板参数定义了基础模板期望从使用者或运行时获得的值。

## 常见模式

基础模板：

```yaml
parameters:
  service_name:
    type: string
    required: true
  environment:
    type: string
    default: dev
  region:
    type: string
    default: eastus
```

子工作流：

```yaml
parameters:
  service_name: procedo
```

运行时覆盖：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## 当前模型允许什么

当前模板模型允许子工作流覆盖：

- 参数值
- 工作流变量
- 顶层执行设置，例如 `max_parallelism` 和 `continue_on_error`
- 工作流 `name`

## 子工作流不能重新定义什么

在当前实现中，子工作流不能定义：

- 新的参数 schema 定义
- 新的 stages
- 新的 jobs
- 新的 steps

## 什么时候使用模板参数

- 环境选择
- 服务或应用命名
- 区域和发布目标控制
- 传入可复用流程的结构化元数据

## 相关内容

- [Templates Overview](./templates-overview.md)
- [Template Limitations](./template-limitations.md)
