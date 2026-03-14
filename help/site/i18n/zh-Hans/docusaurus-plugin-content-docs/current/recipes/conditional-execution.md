---
title: 条件执行
description: 使用运行时条件来决定某个步骤是否执行。
sidebar_position: 3
---

这个示例方案聚焦于 `condition:` 和运行时控制。

## 试一下这个示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=prod
```

同一个示例也可以使用 `environment=dev` 运行，这个变体也已经在当前仓库中验证通过。

## 这个模式为什么有用

- 支持环境相关的行为差异
- 避免复制整份工作流
- 让分支逻辑保持显式且可审查

## 参数变化会带来什么不同

- 当 `environment=prod` 时，生产路径会被允许执行
- 当 `environment=dev` 时，工作流会跳过生产步骤，改走非生产路径

这是一个很好的例子，说明什么时候应该用运行时条件，而不是为每个环境分别维护一份工作流文件。

## 相关内容

- [Conditions](../author-workflows/conditions.md)
- [Validation](../run-and-operate/validation.md)
