# Tasks for: Toolbox Cross-Platform GUI (002-build-gui-application)

Feature goal: Embed an Avalonia-based GUI in the existing Toolbox project to manage Interactive Brokers configuration, orchestrate downloads, and browse LEAN data. Follow TDD-first ordering; tasks marked with [P] can run in parallel when dependencies allow.

## Setup

- [X] Setup moved to `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI`.
  Files: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/*` (UI project contains App, windows, and viewmodels)
  Actions: GUI project and credential helpers were migrated to the UI project. The CLI Toolbox excludes GUI sources and the UI project references the CLI for shared services.


## Tests (TDD)

- Note: Tasks T003–T005 (unit/contract tests) have been deferred and will be created alongside core implementation tasks (T006/T008) following TDD practices. Add specific test tasks back when ready to implement `OutputLayout` validations, data models, and GUI API contract tests.

## Core Implementation

- [ ] **T006** — Implement GUI service layer (in-process API)
- [X] **T006** — Implement GUI service layer (in-process API)
  Files: `Services/GuiService.cs`, `Api/GuiApi.cs`, `Services/DownloadJobManager.cs`, `GuiServiceTests.cs`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (consumer UI project will call into this service)
  Actions: Implement service fulfilling contract using OutputLayout/DataWriter/InteractiveBrokersDownloader; add unit tests.

- [ ] **T007** — Add end-to-end GuiService integration test [P]
  Files: `GuiServiceIntegrationTests.cs`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (integration validates end-to-end from UI consumer to service)
  Actions: Use temp LEAN data to validate snapshot paging and download job lifecycle.

- [ ] **T008** — Build Avalonia UI skeleton tied to service layer
  Files: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/MainWindow.axaml`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/ViewModels/MainViewModel.cs`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/Views/SnapshotView.axaml`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/Views/DownloadView.axaml`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (Avalonia UI project)
  Actions: Bind UI to service, add pagination skeleton and job status controls.

## Integration

- [ ] **T009** — Persist brokerage configuration via credential store
  Files: `Security/CredentialStore.cs`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/ViewModels/ConnectionViewModel.cs`, related tests
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (view model lives in UI project)
  Actions: Wire GUI to secure storage with tests.

- [ ] **T010** — Implement download job persistence & resume logic
  Files: `Services/DownloadJobManager.cs`, `Services/JobStore.cs`, `JobStoreTests.cs`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (UI should surface persisted jobs from the service)
  Actions: Store job metadata securely, resume on startup.

## Polish

- [ ] **T011 [P]** — UI polish & accessibility
  Files: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/Resources/*`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/Styles/*`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (UI resources and styles)
  Actions: Add styles, keyboard shortcuts, platform tweaks.

- [ ] **T012** — Logging & observability improvements
  Files: `StructuredLogger.cs`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/Logging/UiLoggerAdapter.cs`, `LoggingTests.cs`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (UI logging adapter)
  Actions: Add UI-aware logging, ensure secrets not logged.

- [ ] **T013** — Documentation updates
  Files: `specs/002-build-gui-application/quickstart.md`, `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI/Gui/README.md`
  Project: `ToolBox/QuantConnect.InteractiveBrokers.ToolBox.UI` (update UI README and quickstart)
  Actions: Update run instructions, document credential storage and troubleshooting.

## Dependency Highlights

- Complete Setup tasks before Tests/Core work.  
- Tests **T003–T005** should precede implementations that rely on them (T006/T008).  
- UI (T008) depends on T006 and T004.  
- Credential & job persistence (T009/T010) depend on earlier setup and services.  
- Polish tasks follow once core functionality is stable.

## Parallel Guidance

- `[P]` tasks can run concurrently when touching different files. Example: execute all unit tests in parallel via CI matrix:  
  `dotnet test ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests --filter "Category=Unit"`

## Commands Reference

- Create Avalonia project (completed T001):
  
  ```bash
  mkdir -p ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Gui
  dotnet new avalonia.app -o ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Gui --framework net9.0
  dotnet add ToolBox/QuantConnect.InteractiveBrokers.ToolBox/QuantConnect.InteractiveBrokers.ToolBox.csproj reference ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Gui/QuantConnect.InteractiveBrokers.ToolBox.Gui.csproj
  ```

- Run GuiService unit tests (planned for T006):
  
  ```bash
  dotnet test ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests --filter "FullyQualifiedName~GuiServiceTests"
  ```
