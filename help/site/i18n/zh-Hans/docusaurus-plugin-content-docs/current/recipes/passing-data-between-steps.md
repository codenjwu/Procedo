---
title: 在步骤之间传递数据
description: 使用输出和表达式在 Procedo 工作流中传递数据。
sidebar_position: 2
---

当后续步骤依赖前面步骤产生的值时，就应该使用 step outputs。

这是最有价值的基础模式之一，因为它能把一串彼此孤立的步骤变成真正的数据流。

## 试一下这个示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/05_outputs_and_expressions.yaml
```

这个命令已经在当前仓库中验证成功。

## 观察重点

- 一个步骤产生值
- 后续步骤消费这个值
- 表达式绑定把它们连接起来

## 预期结果

关键输出如下：

```text
alpha
from producer: alpha
```

第二行证明后续步骤成功消费了前面步骤的输出。

## 相关内容

- [Outputs](../author-workflows/outputs.md)
- [Conditions](../author-workflows/conditions.md)
