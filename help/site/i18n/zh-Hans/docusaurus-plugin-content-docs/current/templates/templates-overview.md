---
title: 模板概览
description: 使用 Procedo 模板在复用稳定工作流结构的同时，灵活改变参数和变量。
sidebar_position: 1
---

Procedo 模板提供了一种边界明确、行为可预测的复用模型。

当执行图本身比较稳定，而主要差异只是服务名、环境、区域或发布元数据这类值时，模板就非常适合。

## 模板支持什么

当前模板模型主要面向这些场景：

- 可复用的基础工作流
- 参数化的环境或部署差异
- 工作流级变量定制
- 通过 CLI 或宿主传入运行时参数覆盖

## 子工作流示例

仓库里有一个简单的模板使用者示例：

```yaml
template: ./templates/standard_build_template.yaml
name: template_parameters_demo

parameters:
  service_name: procedo

variables:
  artifact_name: "custom-${params.service_name}-${params.environment}"
```

## 基础模板示例

被引用的基础模板定义了可复用的工作流结构：

```yaml
name: standard_build_template
version: 1

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

variables:
  artifact_name: "${params.service_name}-${params.environment}-${params.region}"

stages:
- stage: build
  jobs:
  - job: package
    steps:
    - step: announce
      type: system.echo
      with:
        message: "Building ${vars.artifact_name}"
```

## 已验证命令

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## 如何理解模板的使用方式

- 当执行图基本不变时，优先使用模板
- 使用参数和变量来定制行为
- 当某个已声明步骤只是有时要跳过时，使用运行时 `condition:`
- 不要把模板当成通用图组合系统来使用

## 相关内容

- [Template Parameters](./template-parameters.md)
- [Template Conditions](./template-conditions.md)
- [Template Limitations](./template-limitations.md)
