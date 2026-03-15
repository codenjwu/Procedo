---
title: 内置步骤概览
description: 使用内置 system 步骤目录完成常见工作流任务，而不必自己编写插件。
sidebar_position: 20
---

Procedo 自带一套 `system.*` 内置步骤目录，覆盖常见工作流任务。

当前 system 插件中注册的 step type 包括以下类别：

- 输出与通用工具值
- 等待与恢复行为
- HTTP
- 文件和目录操作
- 哈希与压缩辅助
- JSON、CSV 和 XML 辅助
- 进程执行

## 已注册的内置步骤类型

当前 system 插件注册了这些 step type：

- `system.echo`
- `system.guid`
- `system.now`
- `system.concat`
- `system.sleep`
- `system.wait_signal`
- `system.wait_until`
- `system.wait_file`
- `system.http`
- `system.file_write_text`
- `system.file_read_text`
- `system.file_copy`
- `system.file_move`
- `system.file_delete`
- `system.base64_encode`
- `system.base64_decode`
- `system.hash`
- `system.zip_create`
- `system.zip_extract`
- `system.dir_create`
- `system.dir_list`
- `system.dir_delete`
- `system.json_get`
- `system.json_set`
- `system.json_merge`
- `system.process_run`
- `system.csv_read`
- `system.csv_write`
- `system.xml_get`
- `system.xml_set`

## 如何阅读这一节

- 需要基础工作流辅助步骤时，看 `Core Utilities`
- 需要文件系统相关任务时，看 `File and Directory`
- 需要调用外部可执行文件时，看 `Process and Security`
- 需要运维人员驱动或信号驱动的工作流时，看 `Wait and Resume`

## 相关内容

- [Steps](../author-workflows/steps.md)
- [Templates Overview](../templates/templates-overview.md)
