---
title: 模板循环
description: 使用模板阶段的 each 循环，根据数组输入展开重复的 YAML 节点。
sidebar_position: 4
---

Procedo 当前通过 `${{ each }}` 支持“仅数组”的模板循环。

## 示例

```yaml
steps:
  ${{ each region in params.all_regions }}:
  - step: deploy_${region}
    type: system.echo
    condition: and(in('${region}', params.active_regions), not(contains('${region}', 'east')))
    with:
      message: "Deploy ${vars.release_label} to ${region}"
```

## 这个能力会做什么

这个循环会在运行前展开重复的 YAML 节点。

在上面的例子中：

- `params.all_regions` 里的每个区域都会生成一个步骤
- 每个生成出来的步骤都有区域相关的 step id
- 运行时 `condition:` 仍然决定这些生成步骤是否真正执行

## 已验证示例

```powershell
dotnet run --project src/Procedo.Runtime -- examples/74_control_flow_array_iteration_demo.yaml
```

## 当前限制

当前实现只支持数组迭代。对象或字典迭代不属于这个阶段的能力范围。

## 相关内容

- [Template Conditions](./template-conditions.md)
- [Dependencies and Execution Order](../recipes/dependencies-and-execution-order.md)
