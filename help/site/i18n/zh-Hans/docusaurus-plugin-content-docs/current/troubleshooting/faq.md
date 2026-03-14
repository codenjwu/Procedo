---
title: 常见问题
description: 对 Procedo 常见问题的快速回答。
sidebar_position: 2
---

## 什么时候应该用 Procedo，而不是直接写脚本？

当你希望工作流具备结构化定义、验证能力、持久化、运行时控制以及可检查的执行行为时，就应该考虑 Procedo。

## 我应该从哪里开始学习？

建议从这些页面开始：

- [Install and Setup](../get-started/install-and-setup.md)
- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
- [Create Your First Workflow](../get-started/create-your-first-workflow.md)

## 示例放在哪里？

仓库中的示例都放在 `examples/` 目录下。

## 文档中的代码片段如何保持可信？

这个帮助站点的设计原则是：优先使用已经测试过的示例文件，作为公开代码片段的事实来源。

## 不先写自定义应用，我也能使用 Procedo 吗？

可以。`src/Procedo.Runtime` 里的 CLI 宿主就是最简单的直接运行工作流文件的方式。

## 什么时候应该使用参数，而不是为每个环境分别改 YAML？

当工作流结构不变，只是运行时值发生变化时，就应该使用参数。

## 什么时候应该用 `condition:`，而不是模板？

如果某个步骤仍然属于工作流，只是有时需要在运行时跳过，就使用 `condition:`。如果你希望在运行前就改变生成出来的工作流结构，就使用模板。
