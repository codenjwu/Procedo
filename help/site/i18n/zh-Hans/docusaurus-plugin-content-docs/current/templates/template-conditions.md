---
title: 模板条件
description: 使用模板阶段的分支指令，在运行前包含或排除 YAML 节点。
sidebar_position: 3
---

Procedo 支持这些模板阶段分支指令：

- `${{ if ... }}`
- `${{ elseif ... }}`
- `${{ else }}`

这些指令会在运行时步骤执行开始之前生效。

## 示例

仓库中包含一个分支示例：

```yaml
steps:
  ${{ if eq(params.environment, 'prod') }}:
  - step: branch_prod
    type: system.echo
    with:
      message: "Branch selected production rollout for ${vars.release_label}"
  ${{ elseif eq(params.environment, 'qa') }}:
  - step: branch_qa
    type: system.echo
    with:
      message: "Branch selected QA rollout for ${vars.release_label}"
  ${{ else }}:
  - step: branch_dev
    type: system.echo
    with:
      message: "Branch selected development rollout for ${vars.release_label}"
```

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/59_branching_operator_showcase.yaml
```

## 模板条件与运行时条件的区别

- 当你希望添加或移除 YAML 节点时，使用模板阶段指令
- 当某个已声明步骤仍然保留在工作流中，但运行时可能被跳过时，使用 `condition:`

## 当前实现中的一个细节

当前模板实现会在展开基础模板时，基于当时可见的值解析分支。子工作流的覆盖合并发生在更后面，所以如果某个分支必须可靠地响应子工作流提供的值，更建议使用运行时 `condition:`。

## 相关内容

- [Template Loops](./template-loops.md)
- [Conditions](../author-workflows/conditions.md)
