---
title: 工作流结构概览
description: 了解 stages、jobs 和 steps 如何组合成一个 Procedo 工作流。
sidebar_position: 1
---

Procedo 的工作流 YAML 使用分层结构组织。即使工作流逐渐演变成更大的操作流程，这种结构也能帮助它保持清晰可读。

## 层级结构

Procedo 按照下面的顺序组织工作流定义：

1. workflow
2. stages
3. jobs
4. steps

## 结构示例

```yaml
name: example
version: 1

stages:
- stage: build
  jobs:
  - job: compile
    steps:
    - step: announce
      type: system.echo
      with:
        message: "Building"
```

## 如何理解这段结构

- 一个 workflow 包含一个或多个 stage。
- 每个 stage 包含一个或多个 job。
- 每个 job 包含一个或多个 step。
- 真正执行工作的，是 step。

## 为什么这很重要

这种层级不仅仅是为了可读性，它也影响你如何理解执行过程：

- stage 表示较大的执行阶段
- job 用来组织相关工作
- step 表示真正可执行的动作

当你开始接触更高级的工作流时，这个结构还会成为这些能力的基础：

- 依赖关系
- 输出传递
- 运行时条件
- 持久化与恢复行为

## 这种结构如何扩展

整个仓库都使用这一套结构，从最简单的 hello 示例到更复杂的场景包都是如此。

这意味着随着工作流越来越复杂，你并不需要切换到另一套完全不同的编写模型。

## 相关内容

- [Steps](./steps.md)
- [Parameters](./parameters.md)
- [Outputs](./outputs.md)
