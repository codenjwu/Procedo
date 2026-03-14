---
title: "内置步骤：核心工具"
description: 了解用于输出值、生成 id、拼接字符串和暂停执行的基础内置步骤。
sidebar_position: 21
---

核心工具步骤是最简单的一组 `system.*` 基础能力。

它们适合这些场景：

- 调试输出
- 时间戳与唯一 id
- 简单字符串拼接
- 演示流程或顺序控制中的短暂停顿

## 代表性步骤

- `system.echo`
- `system.now`
- `system.guid`
- `system.concat`
- `system.sleep`

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/31_system_toolbox_demo.yaml
```

这个示例演示了：

- `system.now`
- `system.guid`
- `system.concat`
- `system.sleep`
- `system.echo`

## 预期行为

当前验证输出中，会有一行类似：

```text
timestamp=<utc timestamp> | guid=<generated guid>
```

具体的时间戳和 guid 每次运行都会变化。

## 什么时候使用这些步骤

- `system.echo`：输出消息和轻量级报告
- `system.now`：生成时间戳
- `system.guid`：生成唯一 id
- `system.concat`：构造可读的输出值
- `system.sleep`：在受控场景中做短暂等待

## 相关内容

- [Built-in Steps Overview](./built-in-steps-overview.md)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume.md)
