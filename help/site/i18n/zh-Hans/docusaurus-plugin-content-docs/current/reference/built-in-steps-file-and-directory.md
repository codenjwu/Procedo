---
title: "内置步骤：文件与目录"
description: 使用内置文件系统步骤读取、写入、移动、删除和组织本地文件。
sidebar_position: 22
---

Procedo 包含常见本地文件操作的内置文件系统步骤。

代表性 step type 包括：

- `system.file_write_text`
- `system.file_read_text`
- `system.file_copy`
- `system.file_move`
- `system.file_delete`
- `system.dir_create`
- `system.dir_list`
- `system.dir_delete`

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/32_system_file_ops_demo.yaml
```

这个示例演示了：

- 创建源文件
- 复制它
- 读取内容
- 移动它
- 输出内容
- 删除最终文件

## 预期行为

已验证输出包括：

```text
file-content=Hello from system.file_write_text
```

## 安全说明

文件系统步骤受 system 插件安全选项控制。在当前安全模型中，文件访问可以被完全禁用，也可以被限制在允许的路径根目录之下。

## 相关内容

- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
- [Validation](../run-and-operate/validation.md)
