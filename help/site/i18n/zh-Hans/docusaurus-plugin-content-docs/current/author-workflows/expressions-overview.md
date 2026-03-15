---
title: 表达式概览
description: 了解 Procedo 如何在输入、变量和条件中解析表达式。
sidebar_position: 6
---

表达式是 Procedo 把工作流数据转换成运行时值的方式。

你会在这些地方看到表达式：

- step 输入
- workflow 变量
- 运行时 `condition:` 条件

## 表达式语法

Procedo 的表达式写在 `${...}` 里面。

例如：

```yaml
message: "release=${steps.vars.outputs.message}"
message: "${format('{0}-{1}', params.service_name, params.environment)}"
condition: eq(params.environment, 'prod')
```

## 常见数据来源

根据当前示例和运行时行为，最常用的命名空间包括：

- `params.`：工作流参数
- `vars.`：工作流变量
- `steps.<stepId>.outputs.<name>`：步骤输出

## 支持的表达式模式

当前示例和解析器支持的模式包括：

- 值替换
- 使用 `format(...)` 进行字符串格式化
- 使用 `eq`、`ne`、`and`、`or`、`not` 进行布尔判断
- 使用 `contains`、`startsWith`、`endsWith`、`in` 等集合和字符串辅助函数

## 已验证示例

仓库里有一个专门展示运行时函数的示例：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml
```

## 如何理解表达式的使用方式

- 用表达式把工作流中的数据连接起来
- 保持表达式清晰、可读、目标明确
- 当表达式变长时，把重复的派生值提取到变量里

## 相关内容

- [Variables](./variables.md)
- [Outputs](./outputs.md)
- [Conditions](./conditions.md)
