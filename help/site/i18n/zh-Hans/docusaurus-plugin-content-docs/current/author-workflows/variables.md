---
title: 变量
description: 使用工作流变量先组合一次值，然后在多个步骤中复用。
sidebar_position: 4
---

变量让你可以给“计算得到的值”或“重复出现的值”命名，从而让整个工作流更容易阅读。

当多个步骤都需要同一个派生值时，变量尤其有用，比如发布标签、打包名称或者服务标识。

## 最小可运行示例

仓库里有一个已经验证过的简单示例：先在一个步骤里生成值，再在后续步骤中复用：

```yaml
name: vars_expression_via_step
version: 1
stages:
- stage: vars
  jobs:
  - job: showcase
    steps:
    - step: vars
      type: system.echo
      with:
        message: "v1.2.3"
    - step: announce
      type: system.echo
      depends_on:
      - vars
      with:
        message: "release=${steps.vars.outputs.message}"
```

运行命令：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/06_vars_expression_via_step.yaml
```

## 预期结果

关键输出如下：

```text
v1.2.3
release=v1.2.3
```

## 变量与输出的区别

- 当你希望一个命名值在整个工作流中广泛可用时，使用 workflow `variables`
- 当某个值是在运行时由具体步骤产生时，使用 step outputs

在实际工作流中，二者常常一起使用：

- 参数由调用方传入
- 变量基于这些参数组合出可复用的值
- 输出把步骤结果继续传递给后续步骤

## 变量的典型用途

- 发布标签
- 制品命名
- 环境相关前缀
- 重复使用的消息片段
- 可复用的条件输入

## 相关内容

- [Parameters](./parameters.md)
- [Outputs](./outputs.md)
- [Conditions](./conditions.md)
