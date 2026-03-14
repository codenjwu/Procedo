---
title: 步骤
description: 了解 Procedo 中 step 的定义方式，以及如何给 step 提供输入。
sidebar_position: 2
---

Step 是 Procedo 工作流中最小的可执行单元。

Procedo 中所有真正的工作，最终都是在 step 里完成的。

## 最小可运行示例

```yaml
steps:
- step: announce
  type: system.echo
  with:
    message: "Hello from a step"
```

## 关键字段

- `step` 为该步骤提供标识符。
- `type` 选择具体实现。
- `with` 提供步骤输入值。
- `condition` 可以在运行时决定该步骤是否执行。

## `type` 表示什么

`type` 字段决定运行哪个步骤实现。在当前的入门示例中，你最常见到的是内置的 `system.*` 类型，比如 `system.echo`。

随着 Procedo 扩展，step type 也可以来自：

- 内置插件
- 应用注册的插件
- 宿主程序暴露的自定义步骤

## 输入是如何工作的

`with` 块就是这个 step 的输入负载。每种 step type 决定它能理解哪些输入字段。

以 `system.echo` 为例，最关键的输入字段就是 `message`：

```yaml
with:
  message: "Hello from a step"
```

## 编写 step 的建议

在编写工作流时，一个设计良好的 step 通常应该：

- 只做一件清晰的事情
- 拥有稳定的 step id
- 如果后续步骤需要结果，就显式输出 outputs
- 只在确实需要运行时分支时才使用 `condition:`

## 相关内容

- [Parameters](./parameters.md)
- [Conditions](./conditions.md)
