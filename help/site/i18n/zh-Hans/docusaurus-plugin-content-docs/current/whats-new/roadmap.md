---
id: roadmap
title: 路线图
sidebar_label: 路线图
description: 基于当前仓库路线图和 Phase 1 之后的优先事项，了解 Procedo 可能的下一步演进方向。
---

# 路线图

这个路线图页面是一个面向用户的摘要，用来说明 Phase 1 之后的大方向。它不是对具体发布日期的承诺，但能帮助你理解项目已经明确认定为高价值的改进方向。

## Phase 1 已经建立了什么

Procedo 已经具备扎实的单节点基础：

- YAML workflows with stages, jobs, and steps
- plugin-based execution
- outputs and expression resolution
- persistence and resume
- wait/signal flows
- validation
- observability
- DI integration and custom step registration
- templates, parameters, and workflow variables

## 最近已完成的优先事项

当前路线图指出，这些偏向运维体验的投入已经完成：

- run inspection and diagnostics improvements
- persisted-run cleanup and retention commands
- richer parameter schema validation
- better operator docs and runbooks
- package and release-surface polish

## 未来较可能继续推进的方向

最明确的未来主题包括：

- production usability improvements
- stronger operator ergonomics
- more polished docs and release communication
- carefully scoped authoring/runtime improvements without breaking the current mental model

## 刻意延后的方向

当前路线图也明确指出，以下内容不是近期重点：

- distributed execution and agent orchestration
- large DSL expansion
- fragment-based template composition
- hosted management UI
- artifact-platform features
- broad enterprise approvals/platform features

这对用户是有帮助的，因为它设定了现实预期。Procedo 是从一个稳定的单节点工作流引擎逐步向外扩展，而不是一开始就试图解决所有编排问题。

## 如何阅读这个路线图

随着帮助站点成熟，这一页应该保持简洁、面向用户：

- what changed recently
- what kinds of improvements are most likely next
- what is intentionally out of scope

更详细的实现计划可以继续放在工程文档里，而这一页只保留产品视角下的快速摘要。

## 相关内容

- [Release Notes Index](./release-notes-index)
- [Known Limitations](./known-limitations)
- [Phase 1 Release Notes](./phase-1-release-notes)
