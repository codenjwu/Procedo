---
title: 表达式函数
description: 参考 Procedo 表达式当前支持的辅助函数。
sidebar_position: 3
---

当前 `ExpressionResolver` 实现支持一组实用的辅助函数，用于运行时条件和数值组合。

## 常见函数分组

布尔与比较辅助函数：

- `eq(a, b)`
- `ne(a, b)`
- `and(a, b, ...)`
- `or(a, b, ...)`
- `not(a)`

字符串与集合辅助函数：

- `contains(a, b)`
- `startsWith(a, b)`
- `endsWith(a, b)`
- `in(value, list...)`

格式化辅助函数：

- `format(template, ...)`

## 已验证示例

仓库中包含一个专门展示函数能力的示例：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml
```

这个示例演示了：

- `or`
- `eq`
- `ne`
- `and`
- `not`
- `contains`
- `startsWith`
- `endsWith`
- `in`
- `format`

## 实际建议

- 当你需要生成可读性好的组合标签时，使用 `format`
- 做成员关系判断时，使用 `in`
- 处理简单字符串规则时，使用 `contains`、`startsWith` 和 `endsWith`
- 对于很长的嵌套条件，尽量把可复用值提取到变量里，保持可读性

## 相关内容

- [Expression Condition Rules](./expressions-condition-rules.md)
- [Conditions](../author-workflows/conditions.md)
