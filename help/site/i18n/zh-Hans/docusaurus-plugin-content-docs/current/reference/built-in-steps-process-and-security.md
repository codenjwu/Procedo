---
title: "内置步骤：进程与安全"
description: 谨慎运行外部进程，并理解当前 system 插件的安全控制。
sidebar_position: 23
---

`system.process_run` 允许工作流调用外部可执行文件，但这也是安全敏感度较高的一类内置能力。

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/40_system_process_demo.yaml
```

当前已验证输出包括：

```text
dotnet-version=10.0.200
```

## 示例使用方式

```yaml
- step: dotnet_version
  type: system.process_run
  with:
    file_name: dotnet
    arguments:
    - --version
    timeout_ms: 10000
```

## 当前安全控制

当前 `SystemPluginSecurityOptions` 模型包含：

- `AllowHttpRequests`
- `AllowFileSystemAccess`
- `AllowProcessExecution`
- `AllowUnsafeExecutables`
- `AllowedPathRoots`
- `AllowedHttpHosts`
- `AllowedExecutables`

## 实际建议

- 在受限宿主中，除非确实需要，否则保持禁用进程执行
- 在受控环境中，优先为可执行文件使用 allowlist
- 对外部进程调用设置超时
- 把进程步骤视为比纯数据或工具类步骤风险更高的能力

## 相关内容

- [Built-in Steps: File and Directory](./built-in-steps-file-and-directory.md)
- [Built-in Steps Overview](./built-in-steps-overview.md)
