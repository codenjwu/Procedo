---
id: phase-1-release-notes
title: Phase 1 发布说明
sidebar_label: Phase 1 发布说明
description: 面向用户总结 Procedo 0.1.0 Phase 1 版本，包括亮点、兼容性说明和升级提示。
---

# Phase 1 发布说明

Procedo Phase 1 是第一条经过打磨的单节点发布线，覆盖 YAML 工作流、嵌入式使用、持久化、可观测性以及模板式编写能力。

## 版本

- Version: `0.1.0`
- Release date: `2026-03-12`

## 亮点

- 基于 `stages -> jobs -> steps` 的 YAML 工作流
- 结合输出与表达式解析的依赖感知调度
- 带内置 `system.*` 步骤的插件化运行时
- 本地持久化、等待型工作流以及按 run id 恢复流程
- 面向控制台和 JSONL sink 的结构化执行事件
- 面向可复用工作流编写的模板分支与循环展开
- 更丰富的参数 schema 验证，让可复用工作流更安全

## 这个版本新增了什么

这个版本确立了 Procedo 的主要使用模型：

- run workflows locally from the CLI host
- embed Procedo into .NET applications
- extend the runtime with plugins and custom steps
- persist and resume waiting runs
- validate workflow shape and parameter input before execution

它还扩充了示例目录，因此文档现在可以指向真实、可运行的工作流，而不仅仅是人为构造的片段。

## 重要的运维改进

Phase 1 还带来了多项面向运维人员的改进：

- run inspection and cleanup workflows for persisted runs
- source attribution improvements for template-defined failures
- additive event contract improvements
- clearer package guidance around the intended public surface

## 兼容性说明

当前兼容性策略是刻意保守的：

- workflow DSL changes are additive in the Phase 1 line
- existing required workflow fields remain stable
- event schema changes are additive and preserve `SchemaVersion = 1`
- runtime flag growth is additive rather than destructive

## 升级提示

对已有用户来说，最重要的两点是：

- step outputs should be referenced as `${steps.<stepId>.outputs.<key>}`
- `vars.*` is reserved for workflow variables rather than step outputs

如果你使用持久化和恢复能力，Phase 1 还让参数和工作流变量在“首次执行”和“恢复执行”两条路径上的行为更加一致。

## 验证状态

工程发布说明中记录的 Phase 1 验证状态是：

- unit tests: green
- integration tests: green
- contract tests: green

在这个帮助站点 side project 中，文档中引用的示例也会在发布前通过代码片段命令套件再次验证。

## 升级后建议阅读

- [Install and Setup](../get-started/install-and-setup)
- [Persistence](../run-and-operate/persistence)
- [Validation](../run-and-operate/validation)
- [Built-in Steps Overview](../reference/built-in-steps-overview)
- [Known Limitations](./known-limitations)
