---
title: Procedo CLI 基础
description: 学习 Procedo CLI 中最常用的运行、恢复和检查模式。
sidebar_position: 4
---

Procedo CLI 宿主是运行示例和观察运行时行为的最快方式。

命令的基本结构是：

```powershell
dotnet run --project src/Procedo.Runtime -- [workflow.yaml] [options]
```

## 常见任务

运行一个工作流：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

带参数运行：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

带持久化状态运行：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/runs
```

查看 CLI 帮助：

```powershell
dotnet run --project src/Procedo.Runtime -- --help
```

## 核心选项

当前运行时帮助中，最重要的能力包括：

- `--param <key=value>`：传入运行时参数
- `--persist`：把运行状态写入本地存储
- `--resume <runId>`：恢复一个已持久化的运行
- `--resume-signal <type>`：为等待中的工作流提供恢复信号
- `--state-dir <path>`：指定运行状态的存储目录
- `--list-waiting`：查看当前处于等待状态的运行
- `--show-run <runId>`：查看某个持久化运行的详情
- `--delete-run <runId>`：删除某个已存储的运行状态
- `--events-console`：把结构化事件输出到控制台
- `--events-json <path>`：把结构化事件输出到 JSONL 文件

## 什么时候优先使用 CLI

当你想做这些事情时，优先用 CLI：

- 直接运行某个工作流文件
- 快速验证工作流编写修改
- 测试参数行为
- 试验持久化和恢复流程
- 不写嵌入式应用也能检查运行时状态

## 相关内容

- [CLI Overview](../reference/cli-overview.md)
- [Persistence](../run-and-operate/persistence.md)
