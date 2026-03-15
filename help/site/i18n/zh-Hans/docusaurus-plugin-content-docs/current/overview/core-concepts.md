---
title: 核心概念
description: 了解 Procedo 工作流和运行时的核心组成部分。
sidebar_position: 3
---

Procedo 工作流由少量核心概念组合而成，最终构成更大的执行图。

## Workflow

工作流是用 YAML 定义的完整执行单元。

它包含元数据、可选的参数和变量，以及一个或多个阶段。

## Stage

阶段把相关的 job 组织成一个更大的执行阶段。

## Job

Job 用来组织在运行上属于同一组的步骤。

Job 还可以携带执行策略，比如最大并行度或出错后是否继续。

## Step

步骤是工作流中最小的可执行单元。

每个步骤通常包含：

- a `type`
- optional inputs under `with`
- optional runtime gating with `condition:`
- optional dependencies with `depends_on`

## Parameters

参数允许调用方在运行时向工作流传入输入值。

## Outputs

输出允许一个步骤把结果暴露给后续步骤使用。

这是在工作流中向后传递数据的主要方式。

## 持久化与恢复

Procedo 可以持久化运行状态，因此等待中的流程或被中断的流程可以在之后继续执行。

## 可观测性

Procedo 可以输出结构化执行事件，帮助你了解一次运行过程中到底发生了什么。

## 插件注册表

步骤类型通过插件注册表解析。运行时既可以加载内置插件，也可以加载应用自己提供的扩展。

## 验证

在执行之前，Procedo 会验证工作流结构、依赖关系以及步骤注册情况，这样很多错误都能在运行前被发现。

## 继续阅读

- [Create Your First Workflow](../get-started/create-your-first-workflow.md)
- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
