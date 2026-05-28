# Research: IB Data Download ToolBox

Date: 2025-09-28
Branch: 001-create-a-data

## Decisions

- Decision: C# / .NET 9 console app with System.CommandLine
  - Rationale: Aligns with existing repo TargetFramework and provides robust CLI parsing
  - Alternatives: Custom args parsing (simpler but less ergonomic); Spectre.Console.Cli (nicer UX, extra dep)

- Decision: Use QuantConnect.Lean.Engine and QuantConnect.Brokerages packages where feasible
  - Rationale: Ensures LEAN-compatible data formats and reduces duplication
  - Alternatives: Hand-craft LEAN file formats (risk of drift); reference LEAN source directly (tight coupling)

- Decision: Filesystem LEAN output under configurable data root
  - Rationale: Matches LEAN ToolBox conventions and keeps scope simple
  - Alternatives: Database or cloud storage (out of scope for v1)

- Decision: Rate limiting via exponential backoff with jitter + bounded retries
  - Rationale: Respect IB pacing; avoid thundering herd
  - Alternatives: Fixed sleeps (worse UX); no backoff (violates constitution/perf)

- Decision: v1 asset classes = Equity Minute and Daily (core), optional Futures Minute if trivial
  - Rationale: Common use-cases; keeps scope focused
  - Alternatives: Add Forex/Options now (push to v2 after feedback)

- Decision: No live network calls in CI; provide opt-in integration test with environment variable gate
  - Rationale: Deterministic CI per Constitution
  - Alternatives: Mock IB (complex; may still be flaky)

## Open Questions Resolved

- Credentials source: environment variables preferred; optional JSON config via --config path
- Logging: structured with levels; redact credentials; include run correlation id
- Cross-platform: no OS-specific dependencies; path handling via .NET APIs

## References

- Existing Zerodha ToolBox sample (structure inspiration)
- QuantConnect LEAN data format docs (folder/filename conventions)
- IBKR API pacing rules
