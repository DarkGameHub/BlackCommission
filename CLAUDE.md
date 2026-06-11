# Black Commission — Game Studio Configuration

Indie Unity co-op game developed with the Claude Code Game Studios framework:
49 coordinated subagents and 70+ workflow skills, each owning a specific domain.

> **Project rules of record:** `@AGENTS.md` is authoritative for game vision,
> design pillars, scope, and long-term constraints. Yan Dai is PM and final
> decision maker. Read it before any design or implementation work.

## Technology Stack

- **Engine**: Unity 6 (6000.4.7f1)
- **Language**: C#
- **Networking**: Netcode for GameObjects (host-authoritative)
- **Version Control**: Git
- **Source Layout**: Unity standard — game code lives in `Assets/_Project/Scripts/`,
  NOT in `src/`. The `src/` convention in framework docs maps to `Assets/_Project/`.

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

> The model's training data predates Unity 6. ALWAYS check
> `docs/engine-reference/unity/` before suggesting any Unity API.

## Game Vision & Project Rules

@AGENTS.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question → Options → Decision → Draft → Approval**

- Ask before writing/editing files; show drafts before requesting approval.
- Multi-file changes require explicit approval for the full changeset.
- No commits, no large-asset deletion, no scene-YAML rewrites without user instruction
  (see `@AGENTS.md`).

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for the full protocol.

## Unity Project Notes

- The playable HQ is runtime-generated in `MvpSceneStyleDirector`; Blender assets
  are supporting/imported unless the runtime flow uses them explicitly.
- An MCP bridge to the Unity editor is available (mcp-unity, port 8091 — the
  canonical port is whatever `ProjectSettings/McpUnitySettings.json` says; the Node
  bridge reads that file at spawn) for scene/material/gameobject operations.
  The package is EMBEDDED at `Packages/com.gamelovers.mcp-unity/` with a local
  patch (background-safe message pump); do not revert to the git-cache version.
- Framework management directories live at project root and are NOT Unity assets:
  - `design/` — game design documents (GDDs, entity registry)
  - `docs/` — architecture, engine reference, postmortems
  - `production/` — sprint plans, milestones, session state
  These sit outside `Assets/` so Unity ignores them.

## Getting Started

- New to the framework? Run `/start` for guided onboarding.
- `/project-stage-detect` analyzes the current project state.
- `/help` lists available skills.
