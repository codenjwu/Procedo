---
title: "内置步骤：数据格式"
description: 使用内置的 JSON、CSV、XML 和编码辅助步骤，在工作流内部处理结构化数据转换。
sidebar_position: 22
---

Procedo 包含了常见结构化数据与编码任务的内置步骤。

代表性 step type 包括：

- `system.json_get`
- `system.json_set`
- `system.json_merge`
- `system.csv_read`
- `system.csv_write`
- `system.xml_get`
- `system.xml_set`
- `system.base64_encode`
- `system.base64_decode`

## JSON 示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/37_system_json_demo.yaml
```

已验证输出包括：

```text
json-version=2
```

## CSV 示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/38_system_csv_demo.yaml
```

已验证输出包括：

```text
csv-rows=2
```

## XML 示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/39_system_xml_demo.yaml
```

已验证输出包括：

```text
xml-mode=prod
```

## 编码示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/34_system_encoding_hash_demo.yaml
```

## 什么时候使用这些步骤

- 转换工作流内部的小型结构化负载
- 提取值供后续条件或报告使用
- 不离开工作流即可完成简单结构化文档的往返处理
- 在下游使用前做轻量级编码或解码

## 相关内容

- [Built-in Steps: Archive and Hash](./built-in-steps-archive-and-hash.md)
- [Expressions Functions](./expressions-functions.md)
