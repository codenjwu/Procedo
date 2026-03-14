---
title: "内置步骤：安全运行时"
description: 使用当前 system 插件安全选项收紧内置 system 能力。
sidebar_position: 27
---

内置 `system.*` 目录可以通过 `SystemPluginSecurityOptions` 运行在受限模式下。

## 当前安全选项

当前安全模型包括：

- `AllowHttpRequests`
- `AllowFileSystemAccess`
- `AllowProcessExecution`
- `AllowUnsafeExecutables`
- `AllowedPathRoots`
- `AllowedHttpHosts`
- `AllowedExecutables`

## 已验证安全运行时示例

```powershell
dotnet run --project examples/Procedo.Example.SecureRuntime
```

这个示例配置了一个收紧后的宿主，它会：

- 只允许在批准根目录内进行受控文件访问
- 阻止外部 HTTP
- 阻止进程执行

## 这个示例证明了什么

这个已验证示例会运行一个允许的工作流和一个被阻止的工作流：

- 被允许的文件写入工作流会成功
- 被阻止的进程执行工作流会按预期失败

## 安全护栏

当前实现会强制执行：

- 文件系统路径的宿主 allowlist
- HTTP host 的宿主 allowlist
- 配置后启用的可执行文件 allowlist
- 默认阻止 `cmd`、`powershell`、`pwsh`、`bash`、`sh` 这类 shell 可执行文件，除非显式允许不安全执行

## 什么时候使用安全运行时

- 生产宿主
- 面向运维人员的工作流运行时
- 只应允许有限文件系统或进程访问的环境
- 工作流来自共享来源或半可信来源的场景

## 相关内容

- [Built-in Steps: HTTP](./built-in-steps-http.md)
- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
