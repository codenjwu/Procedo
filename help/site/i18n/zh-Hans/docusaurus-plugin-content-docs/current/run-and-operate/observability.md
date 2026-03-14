---
title: 可观测性
description: 通过控制台输出和结构化事件来检查 Procedo 的执行过程。
sidebar_position: 2
---

Procedo 可以输出结构化执行事件，让你理解一次运行过程中到底发生了什么。

当工作流不再只是简单示例时，可观测性就很重要。你需要知道：

- 什么开始了
- 什么真的执行了
- 什么被跳过了
- 什么失败了
- 什么完成了
- 哪些内容需要稍后继续排查

## 最小可运行示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/18_observability_console_events.yaml --events-console
```

输出 JSONL 事件：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/19_observability_jsonl_events.yaml --events-json .procedo/events.jsonl
```

## 什么时候使用这些选项

- `--events-console` 最适合本地开发和快速查看
- `--events-json` 更适合需要保留或后续处理的结构化事件轨迹

## 它能带来什么

结构化可观测性可以帮助你：

- 调试执行顺序
- 在运行结束后回顾工作流行为
- 保留用于排障的事件历史
- 为未来接入更多 sink 或外部分析做好准备

## 相关内容

- [Persistence](./persistence.md)
- [CLI Overview](../reference/cli-overview.md)
