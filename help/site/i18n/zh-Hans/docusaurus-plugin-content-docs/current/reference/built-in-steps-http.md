---
title: "内置步骤：HTTP"
description: 使用内置 HTTP 步骤执行受控的外部请求，并了解当前安全策略钩子。
sidebar_position: 23
---

`system.http` 让工作流内部可以发起外部 HTTP 调用。

## 当前输入结构

当前实现支持这些输入：

- `url`
- `method`
- `timeout_ms`
- `allow_non_success`
- `headers`
- `body`
- `content_type`

## 当前输出

该步骤会返回这些值：

- `status_code`
- `reason_phrase`
- `is_success`
- `body`
- `headers`

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/33_system_http_demo.yaml
```

已验证输出包括：

```text
status=200
```

## 安全行为

当前 HTTP 步骤受到 `SystemPluginSecurityOptions` 控制：

- `AllowHttpRequests`
- `AllowedHttpHosts`

如果 HTTP 被禁用，该步骤会因策略错误而失败。如果配置了允许的 host，则只允许访问这些 host。

## 实际建议

- 对外部调用显式设置 `timeout_ms`
- 如果运行时只应该访问批准的目标，使用 host allowlist
- 只有当非 2xx 响应本身属于预期控制流时，才使用 `allow_non_success`

## 相关内容

- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
- [Built-in Steps: Secure Runtime](./built-in-steps-secure-runtime.md)
