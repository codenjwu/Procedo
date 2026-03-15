---
title: 创建你的第一个工作流
description: 学习编写 Procedo 工作流所需的最小 YAML 结构。
sidebar_position: 3
---

本页展示一个最小但真正有用的 Procedo 工作流结构，并解释每一部分的作用。

如果你是第一次使用 Procedo，建议在跑通内置 hello 示例之后再阅读这一页。

## 最小可运行示例

```yaml
name: hello_echo
version: 1

stages:
- stage: demo
  jobs:
  - job: simple
    steps:
    - step: hello
      type: system.echo
      with:
        message: "Hello Procedo"
```

## 结构说明

- `name` 为工作流提供稳定标识，会出现在日志和运行状态里。
- `version` 表示工作流文档版本。
- `stages` 定义顶层执行阶段。
- `stage` 为某个工作阶段命名。
- `jobs` 在阶段内部组织工作。
- `job` 为一组相关步骤命名。
- `steps` 包含真正可执行的动作。
- `step` 为动作提供本地标识。
- `type` 选择步骤实现。
- `with` 向步骤传入输入值。

## 为什么结构是分层嵌套的

Procedo 使用分阶段的结构，是为了让工作流在规模变大后仍然保持可读性。

虽然这个示例很小，但它已经遵循了高级工作流也会使用的同一套结构：

1. workflow
2. stage
3. job
4. step

也就是说，这个简单例子教给你的心智模型，之后在条件、输出、持久化和模板等主题里仍然适用。

## 保存并运行工作流

把 YAML 保存到一个文件里，比如 `my-first-workflow.yaml`，然后执行：

```powershell
dotnet run --project src/Procedo.Runtime -- my-first-workflow.yaml
```

## 预期结果

如果工作流有效，并且内置 system 插件可用，你应该会看到：

- 工作流开始执行
- `demo` stage
- `simple` job
- `hello` 步骤执行
- 输出 `Hello Procedo`
- 成功完成的提示

## 初学者常见错误

- 忘记写 `version`
- 把 `steps` 直接写在 `stages` 下，而不是写在 `jobs` 下
- 步骤中漏掉 `type` 字段
- 使用了未注册的 step type

## 下一步

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Steps](../author-workflows/steps.md)
