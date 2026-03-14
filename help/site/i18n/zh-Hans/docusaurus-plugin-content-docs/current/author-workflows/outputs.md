---
title: 输出
description: 让一个步骤暴露值，供后续步骤消费。
sidebar_position: 4
---

输出是步骤在工作流中向后传递有用数据的方式。

这是工作流编写中最重要的模式之一，因为它能让你保持声明式结构，而不是把所有逻辑都塞进一个巨大脚本步骤里。

## 最小可运行示例

下面这个仓库示例已经验证成功：

```yaml
name: outputs_and_expressions
version: 1
stages:
- stage: expressions
  jobs:
  - job: map
    steps:
    - step: producer
      type: system.echo
      with:
        message: "alpha"
    - step: consumer
      type: system.echo
      depends_on:
      - producer
      with:
        message: "from producer: ${steps.producer.outputs.message}"
```

运行命令：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/05_outputs_and_expressions.yaml
```

## 预期结果

关键输出如下：

```text
alpha
from producer: alpha
```

第二个步骤通过表达式 `${steps.producer.outputs.message}` 读取第一个步骤的输出。

## 为什么输出很重要

- 它可以减少重复。
- 它让后续步骤能够使用前面步骤的结果。
- 它帮助工作流保持声明式，而不是过度依赖脚本。

## 这里值得注意的点

- `producer` 会先执行
- `consumer` 依赖 `producer`
- 第二条消息是在运行时用第一个步骤的输出拼装出来的

当某个步骤计算出了路径、标识、哈希值或状态，而后续步骤又需要这些值时，这个模式会特别有用。

## 相关内容

- [Conditions](./conditions.md)
- [Passing Data Between Steps](../recipes/passing-data-between-steps.md)
