---
title: 插件编写概览
description: 了解使用自定义步骤实现来扩展 Procedo 的主要方式。
sidebar_position: 1
---

Procedo 可以通过自定义 step type 进行扩展，这些自定义步骤会接入与内置步骤相同的运行时执行模型。

## 核心契约

基础契约如下：

```csharp
public interface IProcedoStep
{
    Task<StepResult> ExecuteAsync(StepContext context);
}
```

## 主要注册方式

Procedo 当前支持：

- 直接注册 `IProcedoStep`
- 委托注册
- 基于 DI 的激活
- 方法绑定

## 什么时候该实现插件

当出现这些情况时，可以添加自定义步骤：

- 内置 `system.*` 步骤已经不够用
- 你的工作流需要应用特定行为
- 你想构建可复用的运维原语

## 插件编写建议

- 尊重 `context.CancellationToken`
- 当下游表达式依赖输出时，保持输出结构稳定
- 尽量使用对 JSON 友好的输出值
- 使用 `context.Logger` 做运维日志记录

## 已验证示例

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## 相关内容

- [Create a Custom Step](./create-a-custom-step.md)
- [Method Binding](./method-binding.md)
