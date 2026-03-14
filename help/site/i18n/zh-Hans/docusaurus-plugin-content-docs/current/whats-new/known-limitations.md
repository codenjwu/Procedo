---
id: known-limitations
title: 已知限制
sidebar_label: 已知限制
description: 了解 Procedo 当前刻意设定的范围边界，包括单节点执行、基于文件的持久化、模板限制以及可信宿主安全假设。
---

# 已知限制

Procedo Phase 1 被刻意优化为“可靠的单节点执行”，而不是完整的分布式编排。这种聚焦让运行时更容易理解、也更适合运维，但这也意味着有些边界是设计上的选择，而不是暂时遗漏。

## 运行时范围

当前运行时假设包括：

- single-node execution only
- no queue-backed scheduling or distributed worker coordination
- no built-in multi-tenant isolation for untrusted workflows

如果你需要集群编排、横向 worker 租约管理，或平台级租户隔离，那已经超出了当前发布范围。

## 持久化模型

当前持久化模型的假设包括：

- persistence is local and file-backed
- resume semantics are designed for local host recovery
- long-term state migration is intentionally lightweight

持久化非常适合本地自动化和可恢复的运维流程，但它还不是一个分布式持久化方案。

## 模板与 DSL

当前编写层面的限制包括：

- templates support one base template rather than arbitrary graph composition
- template-time `${{ each }}` currently supports arrays rather than full object iteration
- parameter schemas cover practical validation, not full JSON Schema expressiveness
- step outputs still use `${steps.<stepId>.outputs.<key>}` rather than `vars.*`

当你设计可复用工作流库时，这些限制非常重要。更好的做法是让模板保持聚焦、可预测，而不是把它们发展成完整的元编程系统。

## 安全模型

当前安全假设包括：

- `system.*` steps are designed for trusted-host usage
- file, HTTP, and process guardrails exist, but they are not a complete sandbox
- secret-management integration is not yet a built-in end-to-end feature

如果你需要运行不可信的工作流，就应该默认需要在 Procedo 之外增加额外的宿主隔离和策略控制。

## 可观测性

当前可观测性的假设包括：

- structured execution events are available
- console and JSONL sinks are the primary reference path
- a full metrics/tracing backend story is not yet built into Phase 1

## 包发布与使用建议

当前包使用建议包括：

- the intended public package surface is intentionally small
- some internal implementation projects exist in the repo but are not meant to be the main public onboarding path

对大多数用户来说，先从 `Procedo.Hosting` 开始，只有在需要更细粒度控制时，再往下层走。

## 相关内容

- [Support Matrix](./support-matrix)
- [Roadmap](./roadmap)
- [Security Runtime Guidance](../reference/built-in-steps-secure-runtime)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo)
