
# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context
**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [single/web/mobile - determines source structure]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->
```
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   # Implementation Plan: Toolbox Cross-Platform GUI for IB Configuration and Data Review

   **Branch**: `002-build-gui-application` | **Date**: 2025-10-02 | **Spec**: `spec.md`
   **Input**: Feature specification from `/specs/002-build-gui-application/spec.md`

   ## Summary

   Primary requirement: Add a cross-platform GUI into the existing Toolbox project that provides full Interactive Brokers integration (connect, schedule multi-day downloads, batching/retry) and a Lean-data viewer grid. Chosen UI technology: Avalonia (.NET) to support macOS, Ubuntu, and Windows from a single codebase.

   ## Technical Context

   - Language/Version: .NET 9.0
   - Primary Dependencies: AvaloniaUI, System.Text.Json, Microsoft.DataProtection (or platform-specific keyrings), DotNetZip/System.IO.Compression
   - Storage: User-level encrypted JSON for config; LEAN file system for market data
   - Testing: xUnit (existing repo uses xUnit)
   - Target Platform: macOS, Ubuntu (Linux), Windows
   - Project Type: Single repository with an added GUI module integrated into existing Toolbox project
   - Performance Goals: UI should render initial view for a monthly minute dataset under 2s (with virtualization/pagination); downloads should be resumable and throttle-aware.
   - Constraints: Avoid adding a separate product build; embed GUI into Toolbox project and run via `dotnet run --project ToolBox/QuantConnect.InteractiveBrokers.ToolBox -- gui`.

   ## Constitution Check

   - No constitution violations detected for research/design choices.

   ## Project Structure (proposed additions)

   Toolbox GUI will be added under the existing ToolBox project as a new folder:

   ```
   ToolBox/QuantConnect.InteractiveBrokers.ToolBox/
   ├── Gui/
   │   ├── Gui.csproj
   │   ├── App.xaml
   │   ├── MainWindow.xaml
   │   └── Views/ (controls, dialogs)
   └── ... existing code ...
   ```

   ## Phase 0: Research (complete)

   - Output: `research.md` (created)
   - Key decisions: Avalonia selected; encryption approach recommended per OS; integration via in-process service layer.

   ## Phase 1: Design & Contracts (complete)

   - Outputs created:
     - `data-model.md`
     - `contracts/gui-contract.md`
     - `quickstart.md`

   - Post-Design Constitution Check: PASS

   ## Phase 2: Task Planning Approach

   - Strategy documented in plan template (TDD-first, model→services→UI ordering). Use `/tasks` to generate detailed tasks.

   ## Progress Tracking

   - [x] Phase 0: Research complete (/plan)
   - [x] Phase 1: Design complete (/plan)
   - [ ] Phase 2: Task planning complete (/tasks)
   - [ ] Phase 3: Tasks generated (/tasks)
   - [ ] Phase 4: Implementation complete
   - [ ] Phase 5: Validation passed

   ## Artifacts (absolute paths)

   - /Users/arifkhan/github/Lean.Brokerages.InteractiveBrokers/specs/002-build-gui-application/research.md
   - /Users/arifkhan/github/Lean.Brokerages.InteractiveBrokers/specs/002-build-gui-application/data-model.md
   - /Users/arifkhan/github/Lean.Brokerages.InteractiveBrokers/specs/002-build-gui-application/quickstart.md
   - /Users/arifkhan/github/Lean.Brokerages.InteractiveBrokers/specs/002-build-gui-application/contracts/gui-contract.md

   ---
   Plan ready. Run `/tasks` to generate the implementation tasks from Phase 1 outputs.
## Progress Tracking
