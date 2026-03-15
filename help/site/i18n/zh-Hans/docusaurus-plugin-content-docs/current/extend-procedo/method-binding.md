---
title: 方法绑定
description: 把普通 C# 方法注册为 Procedo 步骤实现，并支持输入和服务绑定。
sidebar_position: 3
---

方法绑定让你无需创建完整的 step 类，也能把普通 C# 方法注册成工作流步骤。

## 注册示例

```csharp
registry.RegisterMethod("custom.method_summary", (Func<string, string, PublishOptions, SummaryPayload>)BuildSummary);
```

## 支持的绑定来源

当前方法绑定支持：

- 按参数名绑定工作流输入
- 使用 `[StepInput("...")]` 指定输入别名
- 从扁平或嵌套的 `with:` 输入绑定 POCO
- 通过以下特性显式绑定来源：
  - `[FromStepContext]`
  - `[FromServices]`
  - `[FromLogger]`
  - `[FromCancellationToken]`

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## 什么时候使用方法绑定

- 低样板的应用本地步骤逻辑
- 与单一操作天然匹配的方法签名
- 轻量级的服务和上下文注入

## 相关内容

- [Create a Custom Step](./create-a-custom-step.md)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo.md)
