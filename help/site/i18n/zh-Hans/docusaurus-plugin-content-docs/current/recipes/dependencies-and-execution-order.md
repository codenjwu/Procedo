---
title: 依赖与执行顺序
description: 使用 depends_on 控制步骤顺序，并表达简单的 DAG 模式。
sidebar_position: 4
---

当执行顺序很重要时，就使用 `depends_on`。

它用于表达“某个步骤必须等待另一个步骤完成之后才能执行”。

## 线性依赖示例

仓库里有一个已经验证过的简单线性链示例：

```yaml
name: linear_depends_on
version: 1
stages:
- stage: build
  jobs:
  - job: pipeline
    steps:
    - step: download
      type: system.echo
      with: { message: "download" }
    - step: parse
      type: system.echo
      depends_on:
      - download
      with: { message: "parse" }
    - step: save
      type: system.echo
      depends_on:
      - parse
      with: { message: "save" }
```

运行命令：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/02_linear_depends_on.yaml
```

## 扇出与汇聚示例

仓库里还有一个经过验证的分支示例：

```powershell
dotnet run --project src/Procedo.Runtime -- examples/03_fan_out_fan_in.yaml
```

这个工作流展示了：

- 一个起始步骤
- 两个都依赖起始步骤的并行分支
- 一个等待两个分支都完成后才执行的汇聚步骤

## 什么时候使用依赖

- 某个步骤要消费另一个步骤的输出
- 工作必须按照严格顺序执行
- 你需要分支再汇聚的执行模式
- 最终的打包或汇总步骤需要等待多个前置条件

## 相关内容

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Passing Data Between Steps](./passing-data-between-steps.md)
- [Conditions](../author-workflows/conditions.md)
