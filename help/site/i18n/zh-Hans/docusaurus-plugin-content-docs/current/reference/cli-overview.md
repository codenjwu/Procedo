---
title: CLI 概览
description: 使用 Procedo CLI 宿主运行工作流、传递参数并检查运行时状态。
sidebar_position: 1
---

Procedo CLI 宿主是直接从仓库运行 YAML 工作流的主要入口。

当前运行时帮助把 CLI 描述为一个单节点宿主，基本使用方式如下：

```text
dotnet run --project src/Procedo.Runtime -- [workflow.yaml] [options]
```

## 常见命令

运行工作流：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

按 `runId` 恢复一个运行：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume <runId> --resume-signal continue --state-dir .procedo/runs
```

列出处于等待状态的运行：

```powershell
dotnet run --project src/Procedo.Runtime -- --list-waiting --state-dir .procedo/runs
```

删除一个旧的或已完成的运行记录：

```powershell
dotnet run --project src/Procedo.Runtime -- --delete-run <runId> --state-dir .procedo/runs
```

## 核心能力

- 运行工作流文件
- 提供运行时参数
- 启用持久化运行状态
- 恢复等待中的工作流
- 列出处于等待状态的工作流
- 检查和清理已存储的运行
- 输出结构化事件

CLI 的核心模型仍然是基于 `runId` 的持久化运行管理。

按等待身份进行回调式恢复属于宿主 API 能力，而不是 CLI 直接承担的主流程。

## 验证行为

运行时会在加载工作流之后、执行开始之前完成验证。

如果验证发现错误：

- CLI 会打印验证问题
- 运行不会继续进入执行阶段
- 进程会以退出码 `1` 结束

如果工作流进入等待状态，CLI 会返回退出码 `2`。

## 最重要的选项

- `--param <key=value>`
- `--persist`
- `--resume <runId>`
- `--resume-signal <type>`
- `--state-dir <path>`
- `--list-waiting`
- `--show-run <runId>`
- `--delete-run <runId>`
- `--delete-waiting-older-than <timespan>`
- `--events-console`
- `--events-json <path>`

## 相关内容

- [Procedo CLI Basics](../get-started/procedo-cli-basics.md)
- [Persistence](../run-and-operate/persistence.md)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume)
