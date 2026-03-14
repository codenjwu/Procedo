---
title: "内置步骤：压缩与哈希"
description: 使用压缩、哈希和编码辅助步骤，为工作流制品打包并生成确定性的摘要值。
sidebar_position: 24
---

Procedo 包含内置辅助步骤，可用于打包制品以及生成确定性哈希值。

代表性 step type 包括：

- `system.hash`
- `system.zip_create`
- `system.zip_extract`
- `system.base64_encode`
- `system.base64_decode`

## 哈希与编码示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/34_system_encoding_hash_demo.yaml
```

已验证输出中包含类似下面的 SHA256 行：

```text
sha256=<hash>
```

## 压缩示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/35_system_archive_demo.yaml
```

已验证输出包括：

```text
archive-read=alpha
```

## 什么时候使用这些步骤

- 打包交接制品
- 校验文件完整性
- 为下游传输准备归档包
- 为发布或审计工作流生成摘要输出

## 相关内容

- [Built-in Steps: File and Directory](./built-in-steps-file-and-directory.md)
- [Templates Overview](../templates/templates-overview.md)
