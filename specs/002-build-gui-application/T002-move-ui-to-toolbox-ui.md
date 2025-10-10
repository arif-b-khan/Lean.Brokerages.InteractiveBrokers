# Feature Specification: T002 — Move UI into its own project

**Feature Branch**: `002-move-ui-to-toolbox-ui`

**Created**: 2025-10-09

**Status**: Draft

**Input**: User request — "Move the UI code from QuantConnect.InteractiveBrokers.ToolBox to new project called QuantConnect.InteractiveBrokers.ToolBox.UI."

## Execution Flow (main)

```text
1. Parse the user description from Input.
   → If empty: ERROR "No feature description provided".
2. Extract key concepts: actors (developers), actions (move/refactor), constraints (build, cross-platform), and data (UI assets).
3. Identify ambiguities and mark them for clarification.
4. Create the new project skeleton and move UI source files (XAML, code-behind, views, viewmodels used only by UI) into it.
5. Update namespaces, x:Class attributes, and Program/App entry points to the new project.
6. Add the new project to the solution and make its ProjectReference conditional if needed to keep CI/OS builds stable.
7. Run full solution build and targeted UI project build on supported platforms.
8. Update documentation and CI if the UI project should be built in CI.
9. Verify tests and acceptance scenarios; address any issues (duplicate XAML names, missing resources).
10. Return: SUCCESS (spec ready for planning and implementation).
```

---

## Quick Guidelines

- Focus on WHAT the change delivers and WHY (developer usability, modularity, build isolation).

- Avoid prescribing low-level implementation details in the spec — keep the document implementation-agnostic where possible.

## User Scenarios & Testing (mandatory)

### Primary User Story

As a developer, I want the UI code separated into its own project so the ToolBox console and UI can be maintained, built, and released independently.

### Acceptance Scenarios

1. Given the repository, when the refactor is complete, then a new project `QuantConnect.InteractiveBrokers.ToolBox.UI` exists and contains the UI sources previously under `ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Gui`. The solution or README documents how to build/run the UI.

2. Given the new UI project, when building the solution with default settings, then the rest of the solution (non-UI projects) builds cleanly. Building the UI project may be opt-in (MSBuild property) until platform-specific issues are resolved.

### Edge Cases

- Avoid Avalonia duplicate x:Class errors by ensuring only one XAML resource declares a given x:Class/logical name.

- Ensure platform-specific APIs used by the UI (e.g., data protection) are either made cross-platform or the UI build is documented as platform-limited.

## Requirements (mandatory)

### Functional Requirements

- FR-001: Create a new .NET project `QuantConnect.InteractiveBrokers.ToolBox.UI` under `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/` containing all UI files (XAML, .axaml.cs, views, viewmodels intended only for the UI).

- FR-002: The new UI project MUST build independently with `dotnet build` for supported platforms, or clearly document required platform constraints.

- FR-003: The root `QuantConnect.InteractiveBrokers.ToolBox` project MUST not produce duplicate compile items that include the same XAML resources (preventing Avalonia AVLN:0002 errors).

- FR-004: Provide a mechanism to make the UI ProjectReference opt-in (e.g., `Condition="'$(BuildingGui)'=='true'"`) or document how to include it in local development/CI.

- FR-005: Update namespaces, x:Class attributes, and Program/App entry points so the UI functions from the new project without collisions.

- FR-006: Existing unit tests and non-UI projects must continue to pass build and tests after the move.

- FR-007: The new UI project MUST reference the `QuantConnect.InteractiveBrokers.ToolBox` CLI project (ProjectReference) so the UI can reuse its data-fetching commands/APIs rather than duplicating download logic.

### Questions / Ambiguities (need clarification)

- [NEEDS CLARIFICATION] Should the original `Gui` folder be removed after the move, or kept as a compatibility shim for a transitional period?

- [NEEDS CLARIFICATION] Should the UI project be built by default in CI, or remain opt-in until the team confirms cross-platform compatibility?

## Key Entities

- UI Project: project artifact that contains windows, views, viewmodels and App entrypoint. No data model changes required by this task.

## Review & Acceptance Checklist

- Content Quality
  - [ ] Spec focuses on user/developer value and does not prescribe unnecessary low-level implementation.
  - [ ] Mandatory sections are present and clear.

- Requirement Completeness
  - [ ] No `[NEEDS CLARIFICATION]` items remaining, or they are tracked and answered.
  - [ ] Requirements are testable (builds, no duplicate XAML names, cross-platform constraints documented).

## Execution Status

- [ ] New UI project skeleton created
- [x] New UI project skeleton created (`ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI`)
- [x] Files moved and namespaces updated (migrated from temporary GuiRun into the new project)
- [x] Solution references updated (UI implemented as a separate project; CLI NOT referencing UI to avoid circular ProjectReference)
- [x] Solution and UI project build verified on at least one platform (local build succeeded)
- [x] README/DEV docs updated with run/build instructions for the UI (`ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/README.md`)

Notes on progress:

- I created `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` and moved the minimal GUI files into it.
- The UI project references the CLI project via a ProjectReference so it can call into existing data-fetching logic.
- I updated `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.csproj` to include a conditional ProjectReference to the new UI project when `BuildingGui=true`.
- VS Code launch and build tasks were updated to point to the new UI project's DLL and csproj.

Notes on circular dependency:

- The UI project references the CLI project (one-way) so the UI can reuse download logic. Adding a ProjectReference in the opposite direction (CLI -> UI) created a circular dependency during restore/build; I removed that back-reference to avoid the circular graph. The UI still builds as part of the solution because it references the CLI.


---

Notes:

- Implementation guidance (for implementers): move files, update namespaces, create `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/QuantConnect.InteractiveBrokers.ToolBox.UI.csproj` with Avalonia dependencies, and make its inclusion opt-in until duplicates and platform issues are validated.
