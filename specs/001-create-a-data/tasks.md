# Tasks: Interactive Brokers Data Download ToolBox Sample

**Input**: Design documents from `/specs/001-create-a-data/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)

```text
1. Load plan.md from feature directory
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
   → quickstart.md: Extract scenarios → integration tests
3. Generate tasks by category:
   → Setup: project init, dependencies, solution wiring
   → Tests: contract tests, integration tests (opt-in)
   → Core: models, services, CLI command(s), writers
   → Integration: logging, rate limiting, gateway host/port
   → Polish: unit tests, docs, quickstart validation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Provide parallel execution examples
7. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`

- [P]: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Phase 3.1: Setup

- [ ] T001 Create folder structure for ToolBox console and tests projects:
      - ToolBox/QuantConnect.InteractiveBrokers.ToolBox/
      - ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/

- [ ] T002 Initialize ToolBox console project at ToolBox/QuantConnect.InteractiveBrokers.ToolBox/QuantConnect.InteractiveBrokers.ToolBox.csproj
      - TargetFramework net9.0
      - PackageReference: System.CommandLine (latest stable compatible with net9.0)
      - PackageReference: QuantConnect.Lean.Engine 2.5.*
      - PackageReference: QuantConnect.Brokerages 2.5.*
      - PackageReference: QuantConnect.IBAutomater 2.0.85 (PrivateAssets analyzers;build)

- [ ] T003 Initialize tests project at ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/QuantConnect.InteractiveBrokers.ToolBox.Tests.csproj
      - TargetFramework net9.0
      - PackageReference: xunit, xunit.runner.visualstudio, FluentAssertions
      - ProjectReference to ToolBox/QuantConnect.InteractiveBrokers.ToolBox/QuantConnect.InteractiveBrokers.ToolBox.csproj

- [ ] T004 Add both projects to solution QuantConnect.InteractiveBrokersBrokerage.sln
      - Ensure build order: library → toolbox → tests

- [ ] T005 [P] Create ToolBox/QuantConnect.InteractiveBrokers.ToolBox/README.md with usage and notes (link to specs/001-create-a-data/quickstart.md)

## Phase 3.2: Tests First (TDD)

- [ ] T006 [P] Create tests: ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/CliParsingTests.cs
      - Verifies required options (--symbol, --security-type, --resolution, --from, --to, --data-dir)
      - Verifies optional options (--exchange, --currency, --config, --log-level)
      - Asserts --help prints usage and exits 0; missing required args exits non-zero

- [ ] T007 [P] Create tests: ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/OutputLayoutTests.cs
      - Validates LEAN equity paths for Minute and Daily resolutions (e.g., data/equity/usa/minute/a/aapl/...)
      - Validates filename casing and partitioning rules

- [ ] T008 [P] Create tests: ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/ConfigSchemaTests.cs
      - Ensures config loader rejects when required keys (IB_USERNAME, IB_PASSWORD, IB_ACCOUNT) are missing
      - Ensures env vars override file when both present; secrets not logged

- [ ] T009 [P] Create tests: ToolBox/QuantConnect.InteractiveBrokers.ToolBox.Tests/IntegrationSmokeTest.cs
      - Skipped by default unless env IB_TOOLBOX_IT=1
      - Invokes Program with sample args; asserts logs and dry run behavior without network

- [ ] T010 Run tests to confirm they fail prior to implementation (document failing assertions)

## Phase 3.3: Core Implementation (ONLY after tests are failing)

- [ ] T011 Implement CLI entrypoint: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Program.cs
      - Use System.CommandLine to define options and parse args
      - Validate inputs; return non-zero on invalid
      - Wire basic structured logging with levels from --log-level

- [ ] T012 [P] Implement Output layout helper: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/OutputLayout.cs
      - Methods: GetPath(request), GetFilename(request, date), SerializeBar(...)
      - Support Equity Minute and Daily (v1); ensure LEAN-compatible folders/files

- [ ] T013 [P] Implement Config loader: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/ConfigLoader.cs
      - Load from env and/or JSON file (contracts/config.schema.json as reference)
      - Validate required keys present; redact sensitive values in logs

- [ ] T014 [P] Implement Backoff policy: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/BackoffPolicy.cs
      - Exponential backoff with jitter; configurable max retries

- [ ] T015 Implement downloader abstraction: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/IDataDownloader.cs and InteractiveBrokersDownloader.cs
      - Define interface for FetchBars(request)
      - Provide initial stub or thin wrapper over existing IB client types in QuantConnect.InteractiveBrokersBrokerage/Client/

- [ ] T016 Implement writer: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/DataWriter.cs
      - Stream bars to CSV using LEAN format; atomic writes (temp then move)
      - Return file list for result

- [ ] T017 Wire command handler: in Program.cs
      - Compose: ConfigLoader → Downloader → DataWriter → Result handling
      - Apply BackoffPolicy around IB requests; log pacing/backoff events

- [ ] T018 Respect date ranges and market sessions
      - Skip non-trading days where applicable; align bar boundaries per exchange
      - Note: initial v1 can defer full market-hours logic if QC helpers not readily available

## Phase 3.4: Integration

- [ ] T019 Logging improvements: ToolBox/QuantConnect.InteractiveBrokers.ToolBox/Logging.cs (or inline)
      - Structured, leveled logs; correlation id per run; redacted secrets

- [ ] T020 Gateway connection options
      - Support --gateway-host and --gateway-port flags (defaults per config.schema)
      - Attempt connection and report actionable errors

- [ ] T021 Optional IBAutomater integration
      - Behind flag --use-ib-automater; no-ops in CI; document in README

## Phase 3.5: Polish

- [ ] T022 [P] Unit tests for additional validations (dates ordering, symbol casing normalization)

- [ ] T023 [P] Performance microbenchmark or timing logs for serialization throughput
- [ ] T024 [P] Update ToolBox README and root README.md with usage section and link to Quickstart
- [ ] T025 Run full unit test suite and ensure deterministic behavior (no external calls)
- [ ] T026 Ensure .specify agent context is up-to-date if new tech added (run update-agent-context script)

## Dependencies

- Setup (T001–T005) before Tests and Core
- Tests (T006–T010) must be written and failing before Core (T011–T018)
- OutputLayout (T012) and ConfigLoader (T013) independent → [P]
- BackoffPolicy (T014) independent → [P]
- Downloader (T015) before Writer wiring (T017)
- Writer (T016) before Command handler wiring (T017)
- Integration tasks (T019–T021) after Core
- Polish (T022–T026) after Integration

## Parallel Example

```text
# Launch tests-first tasks in parallel:
Task: "T006 CliParsingTests.cs"
Task: "T007 OutputLayoutTests.cs"
Task: "T008 ConfigSchemaTests.cs"
Task: "T009 IntegrationSmokeTest.cs"

# Launch core utility implementations in parallel:
Task: "T012 OutputLayout.cs"
Task: "T013 ConfigLoader.cs"
Task: "T014 BackoffPolicy.cs"

# Launch polish tasks in parallel:
Task: "T022 Additional validation tests"
Task: "T023 Perf microbenchmark"
Task: "T024 Docs updates"
```

## Notes

- [P] tasks touch different files with no ordering dependency
- Integration smoke test remains skipped in CI to preserve determinism
- Prefer using QC packages for LEAN data formatting to avoid drift
