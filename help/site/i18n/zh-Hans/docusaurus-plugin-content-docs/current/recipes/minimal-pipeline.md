---
title: 最小流水线
description: 从你能成功运行的最小 Procedo 工作流开始。
sidebar_position: 1
---

这个示例方案给你一个最小但真正有用的 Procedo 工作流。

它非常适合作为新工作流文件的复制和修改起点。

## 工作流

```yaml
name: hello_echo
version: 1

stages:
- stage: main
  jobs:
  - job: hello
    steps:
    - step: say_hello
      type: system.echo
      with:
        message: "Hello from Procedo"
```

## 运行它

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

## 为什么这是一个好的起点

- 它包含了完整的最小工作流结构
- 它足够小，一次阅读就能理解
- 它已经使用了真实的内置 step type
- 它为你尝试新字段提供了一个安全起点

## 第一批建议尝试的修改

成功跑通之后，可以一次只改一个点进行尝试：

- 修改工作流名称
- 修改消息内容
- 添加第二个步骤
- 添加参数，并在消息里引用它

## 相关内容

- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
- [Create Your First Workflow](../get-started/create-your-first-workflow.md)
