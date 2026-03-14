---
id: support-matrix
title: 支持矩阵
sidebar_label: 支持矩阵
description: 查看 Procedo 当前的目标框架覆盖范围、宿主支持假设以及建议的公共包集合。
---

# 支持矩阵

本页总结 Procedo 在当前 Phase 1 发布线下的支持情况。

## 库目标框架

Procedo 的主要库包当前支持多目标框架：

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

这些覆盖适用于主要的公共包：

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

## CLI 宿主支持情况

参考 CLI 宿主 `Procedo.Runtime` 是整个示例和帮助站点代码片段中面向操作人员的宿主。当前仓库文档中，它的目标框架为：

- `net8.0`

在当前工作区中，帮助文档里的示例也会使用仓库当前的 SDK 和工具链来运行这个 CLI 宿主，并通过示例套件验证。

## 契约覆盖

仓库支持文档中特别说明了这些兼容性覆盖：

- `net6.0`
- `net8.0`
- `net10.0`

这些覆盖的目标，是保护公共契约面以及序列化运行时/事件的兼容性。

## 运行模型假设

当前支持的运行模型是：

- single-node execution
- local file-backed persistence
- trusted-host deployment assumptions
- no distributed scheduling or worker coordination

这些假设和目标框架支持同样重要。即使某个目标框架技术上兼容，也不代表运行时范围会因此改变。

## 公共包建议

对大多数用户，推荐的入口包是：

- `Procedo.Hosting`
- `Procedo.Plugin.System`

然后只在需要时再补充这些包：

- `Procedo.Engine` for lower-level execution control
- `Procedo.Plugin.SDK` for custom plugin authoring
- `Procedo.Extensions.DependencyInjection` for `IServiceCollection` integration

## 相关内容

- [Package Overview](/)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo)
- [Known Limitations](./known-limitations)
- [Phase 1 Release Notes](./phase-1-release-notes)
