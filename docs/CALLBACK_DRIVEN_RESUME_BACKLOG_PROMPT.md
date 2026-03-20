# Callback-Driven Resume Backlog Prompt

Use the following prompt in the Procedo implementation thread to turn the callback-driven resume requirements into a backlog.

---

You are a senior .NET architect and workflow engine engineer working on Procedo.

Before doing anything else, read and follow the relevant Procedo docs and current code.

Use this requirements document as the source of truth:

- [CALLBACK_DRIVEN_RESUME_REQUIREMENTS.md](./CALLBACK_DRIVEN_RESUME_REQUIREMENTS.md)

Your task is:

Create a Jira-style backlog for implementing generic callback-driven resume support in Procedo.

Important constraints:

- Keep the solution generic and engine-level.
- Do not add ProtoScope-specific types or behavior.
- Do not add HTTP, webhook, WebSocket, or UI logic.
- Do not redesign Procedo around a product-specific use case.
- Focus on reusable wait/query/resume engine capabilities only.

Backlog expectations:

- Create one epic for callback-driven resume support.
- Break the work into clear implementation stories.
- Include technical stories for:
  - active wait query model,
  - waiting-run query APIs,
  - resume-by-wait-identity APIs,
  - duplicate-match behavior,
  - concurrency-safe resume,
  - persistence/store support,
  - engine tests,
  - documentation updates.
- If needed, include follow-up stories for compatibility and migration notes.

For each story include:

- ID
- Title
- Goal
- Description
- Acceptance Criteria
- Dependencies
- Risks or notes where relevant

Output format:

1. First explain the implementation approach at a high level.
2. Second provide the epic.
3. Third provide the ordered Jira-style stories.
4. Fourth identify the minimum viable implementation slice versus follow-up enhancements.

Quality bar:

- Production-grade
- Clean architecture
- Strong boundaries
- No product-specific leakage
- No unnecessary abstractions

---

If needed, you may also propose a second pass prompt for:

- implementing the backlog items in order, one module at a time,
- or reviewing the final design before code generation.
