---
id: introduction
title: Introduction
description: Learn what Procedo is, what problems it solves, and how this help site is organized.
slug: /
sidebar_position: 1
---

Procedo is a .NET workflow engine for YAML-defined pipelines with dependency-aware execution, plugin-based steps, persistence, callback-capable resume, and observability.

It is designed for cases where you want workflows to be reviewable, reproducible, and easier to operate than a loose collection of scripts.

This help site is designed to be practical first:

- short explanations
- tested examples
- task-oriented how-to pages
- precise reference material

## What You Can Do With Procedo

- Define workflows in YAML using stages, jobs, and steps.
- Pass values between steps with outputs and expressions.
- Gate execution with runtime `condition:` checks.
- Reuse workflow structure with templates and template-time branching/iteration.
- Persist and resume long-running or approval-driven flows, including host-driven callback-style resume.
- Extend the engine with plugins and custom steps.

## Who This Help Site Is For

- users authoring YAML workflows
- engineers embedding Procedo inside .NET applications
- operators running and inspecting workflow executions
- plugin authors extending the runtime

## What Makes This Documentation Different

This site is intended to become a user reference, not just a project overview.

That means:

- examples should be runnable
- commands should be validated against the current repository
- large topics should be split into smaller, searchable pages
- descriptions should explain behavior, not just list fields

## Start Here

If you are new to Procedo, go to:

- [Install and Setup](../get-started/install-and-setup.md)
- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
- [Create Your First Workflow](../get-started/create-your-first-workflow.md)

## How This Site Is Organized

- `Overview` explains concepts and positioning.
- `Get Started` helps you reach the first successful run quickly.
- `Author Workflows` covers YAML authoring.
- `Run and Operate` covers validation, persistence, and observability.
- `Use in .NET` covers host-builder composition, callback-driven resume, and embedding patterns.
- `Reference` provides compact syntax and behavior pages.
- `Recipes` provides tested examples you can adapt.
