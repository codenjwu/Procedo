---
title: 常见验证错误
description: 了解常见的验证失败类型，以及当工作流被拒绝时应该从哪里开始排查。
sidebar_position: 1
---

验证错误通常是工作流定义不完整或不一致时，最早出现的信号。

## 常见类别

- 缺少插件或步骤实现
- 依赖环
- 未知依赖
- 非法参数输入

## 真实示例

仓库里包含了一些故意构造的非法工作流，用来演示这些失败场景：

- `examples/13_missing_plugin_validation_error.yaml`
- `examples/14_cycle_dependency_validation_error.yaml`
- `examples/15_unknown_dependency_validation_error.yaml`

## 当前错误消息

当前运行时会输出类似下面的消息：

```text
[ERROR] PV304 ... No plugin registered for step type 'no.such.plugin'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

以及：

```text
[ERROR] PV309 ... Cyclic dependency detected in stage 'validate', job 'cycle'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

## 应该如何处理

- 先阅读摘要行之前出现的第一条验证错误
- 在排查运行时行为之前，先修复结构或依赖问题
- 确认 step type 已经正确注册
- 检查 `depends_on` 是否有拼写错误或形成了环
- 每修复一个问题后重新运行一次工作流

## 相关内容

- [Validation](../run-and-operate/validation.md)
- [FAQ](./faq.md)
