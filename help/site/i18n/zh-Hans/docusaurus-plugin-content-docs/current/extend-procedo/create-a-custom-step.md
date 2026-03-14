---
title: 创建自定义步骤
description: 通过委托、类实现或 DI 注册的方式添加自定义 Procedo 步骤。
sidebar_position: 2
---

学习自定义步骤注册的最快方式，就是直接使用仓库中那个在同一个宿主里演示多种注册方式的示例。

## 已验证示例应用

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## 这个示例演示了什么

当前示例注册了：

- 一个基于委托的步骤
- 一个基于 DI 的 `IProcedoStep`
- 带显式绑定特性的 method-bound 步骤

## 委托示例

```csharp
registry.Register("custom.delegate_hello", context => new StepResult
{
    Success = true,
    Outputs = new Dictionary<string, object>
    {
        ["greeting"] = $"Delegate hello, {context.Inputs["name"]}",
        ["name"] = context.Inputs["name"]
    }
});
```

## 基于 DI 的步骤示例

```csharp
registry.Register<DiHelloStep>("custom.di_hello");
```

## 什么时候选择哪一种方式

- 对于小型、应用内部的逻辑，使用委托
- 对于更大或可复用的行为，使用 `IProcedoStep`
- 当步骤需要依赖服务时，使用基于 DI 的步骤

## 相关内容

- [Plugin Authoring Overview](./plugin-authoring-overview.md)
- [Dependency Injection Integration](./dependency-injection-integration.md)
