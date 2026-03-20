---
id: phase-1-release-notes
title: Phase 1 发布说明
sidebar_label: Phase 1 发布说明
description: 面向用户总结 Procedo 1.0.0-rc1 Phase 1 版本，包括亮点、兼容性说明和升级提示。
---

# Phase 1 发布说明

Procedo Phase 1 RC 是第一条经过打磨的单节点发布线，覆盖 YAML 工作流、嵌入式使用、持久化、回调式恢复、可观测性以及模板式编写能力。

## 版本

- Version: `1.0.0-rc1`
- Release date: `2026-03-20`

## 亮点

- 基于 `stages -> jobs -> steps` 的 YAML 工作流
- 结合输出与表达式解析的依赖感知调度
- 带内置 `system.*` 步骤的插件化运行时
- 本地持久化、活动等待查询、按 run id 恢复以及按等待身份的回调式恢复
- 面向控制台和 JSONL sink 的结构化执行事件，以及更清晰的恢复回放语义
- 面向可复用工作流编写的模板分支、仅数组循环展开与运行时 `condition:` 控制
- 更丰富的参数 schema 验证，让可复用工作流更安全
- 更广泛的可执行示例目录以及更完整的嵌入式示例项目

## 这个版本新增了什么

这个版本确立了 Procedo 的主要使用模型：

- 从 CLI 宿主在本地运行工作流
- 把 Procedo 嵌入到 .NET 应用中
- 通过插件和自定义步骤扩展运行时
- 持久化并恢复等待中的运行
- 在宿主代码中查询活动等待并按等待身份恢复
- 在执行前验证工作流结构和参数输入

它还扩充了示例目录，因此文档现在可以指向真实、可运行的工作流，而不仅仅是人为构造的片段。

## 重要的运维改进

Phase 1 还带来了多项面向运维人员的改进：

- run inspection and cleanup workflows for persisted runs
- source attribution improvements for template-defined failures
- additive event contract improvements
- clearer package guidance around the intended public surface
- 持久化和非持久化执行路径之间的策略一致性加固

## 兼容性说明

当前兼容性策略是刻意保守的：

- Phase 1 线上的工作流 DSL 变更保持为增量式
- 现有必填工作流字段保持稳定
- 事件 schema 变更是增量式，并保持 `SchemaVersion = 1`
- 运行时 flag 的增长保持为增量式而不是破坏式
- 自定义 store 的采用方式仍然是通过能力接口逐步扩展，而不是强制重写基础契约

## 升级提示

对已有用户来说，最重要的两点是：

- step outputs 应该写成 `${steps.<stepId>.outputs.<key>}`
- `vars.*` 保留给工作流变量，而不是步骤输出
- `${{ each }}` 在当前阶段刻意只支持数组
- 回调式恢复依赖持久化工作流快照，以确保恢复时使用安全且一致的工作流定义

如果你使用持久化和恢复能力，Phase 1 还让参数和工作流变量在“首次执行”和“恢复执行”两条路径上的行为更加一致。

## 验证状态

这次 RC 的工程验证结果是：

- unit tests: `250/250 passed`
- integration tests: `145/145 passed`
- contract tests: `57/57 passed`，覆盖 `net6.0`、`net8.0` 和 `net10.0`

帮助站点中引用的示例和嵌入式项目，也已经通过示例治理和 smoke 测试重新验证。

## 升级后建议阅读

- [Install and Setup](../get-started/install-and-setup)
- [Persistence](../run-and-operate/persistence)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume)
- [Validation](../run-and-operate/validation)
- [Built-in Steps Overview](../reference/built-in-steps-overview)
- [Known Limitations](./known-limitations)
