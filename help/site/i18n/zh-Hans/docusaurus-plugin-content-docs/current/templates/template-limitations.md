---
title: 模板限制
description: 了解当前 Procedo 模板模型刻意设定的边界。
sidebar_position: 5
---

Procedo 模板在当前阶段是刻意受限的。

这样可以保持模板行为可预测，但也意味着模板并不是一个完整的继承系统或片段组合系统。

## 当前限制

当前实现不支持：

- 任意 stage、job 或 step 合并
- 模板片段导入
- 多重模板继承
- `${{ each }}` 的对象或字典迭代
- 面向未来复杂图组合能力的完整源码映射

## 子工作流限制

使用基础模板时，子工作流不能定义：

- 新的 stages
- 新的 jobs
- 新的 steps
- 新的参数 schema 定义

如果子工作流尝试添加这些内容，模板加载就会失败。

## 实际建议

模板适合这些场景：

- 稳定的构建、打包、发布流程
- 标准化的部署结构
- 主要差异只在值而不在结构的工作流

模板目前还不太适合这些场景：

- 高度动态的执行图组合
- 需要大量结构注入的工作流
- 多模板装配模式

## 相关内容

- [Templates Overview](./templates-overview.md)
- [Template Parameters](./template-parameters.md)
