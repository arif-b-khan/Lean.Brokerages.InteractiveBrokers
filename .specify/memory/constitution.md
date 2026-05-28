<!--
Sync Impact Report
- Version change: 0.0.0 → 1.0.0
- Modified principles: N/A (initial adoption)
- Added sections: Core Principles (5), Engineering Standards, Development Workflow & Quality Gates, Governance
- Removed sections: None
- Templates requiring updates:
  ✅ .specify/templates/plan-template.md (footer version/path)
  ✅ .specify/templates/spec-template.md (reviewed, no change)
  ✅ .specify/templates/tasks-template.md (reviewed, no change)
  ✅ .specify/templates/agent-file-template.md (reviewed, no change)
- Follow-up TODOs: None
-->

# Lean.Brokerages.InteractiveBrokers Constitution

## Core Principles

### I. Behavior Parity with LEAN and IB APIs (NON-NEGOTIABLE)

The brokerage plugin MUST preserve functional parity with upstream LEAN expectations
and Interactive Brokers (IB) API behavior.

- Public contracts, error codes, and order semantics MUST NOT change without a
  major version bump and migration notes.
- Brokerage models, fees, margin, fills, and instrument mapping MUST match the
  documented IB behavior for supported asset classes.
- Synchronization with upstream LEAN changes MUST be validated by tests before release.

Rationale: Predictable parity enables reproducible research, safe live trading, and
smooth upgrades across LEAN and brokerage layers.

### II. Tests-First and Deterministic CI Gate

All changes MUST be backed by deterministic tests that run in CI.

- Unit and integration tests MUST be added or updated for every functional change.
- Tests MUST avoid nondeterminism (fixed clocks, seeded randomness, stable data).
- CI MUST build and run the full test suite; merges are blocked on failures.
- Edge cases (timeouts, re-connects, partial fills, rate limits) MUST be covered
  with targeted tests.

Rationale: Deterministic tests guard live trading reliability and prevent regressions.

### III. Backward Compatibility and Semantic Versioning

The project MUST follow SemVer for releases.

- PATCH: Bug fixes only, no public contract changes.
- MINOR: Backward compatible additions; deprecations require clear warnings and
  a documented removal window.
- MAJOR: Any breaking change to public contracts or behavior; requires migration notes.

Rationale: Clear versioning and deprecation paths enable safe adoption in production.

### IV. Observability and Operability

The plugin MUST provide actionable telemetry without leaking sensitive data.

- Use structured, leveled logging for order lifecycle, connectivity, and errors.
- No secrets or PII in logs; redact credentials and account identifiers.
- Expose correlation identifiers for tracing multi-step operations.
- Timeouts and retries MUST be visible via logs/metrics for production triage.

Rationale: Fast incident response requires traceable, safe, and useful signals.

### V. Performance, Concurrency, and Safety for Live Trading

The runtime MUST be efficient, thread-safe, and resilient.

- Avoid blocking calls on hot paths; prefer async/cancellable operations.
- Enforce thread-safety for shared state (orders, positions, subscriptions).
- Respect IB rate limits and backoff guidance.
- Never swallow errors; surface actionable failures with context.

Rationale: Live trading demands low-latency, predictable behavior with safe recovery.

## Engineering Standards

- Language/Runtime: C# targeting the version aligned with upstream LEAN; keep
  dependencies minimal and pinned.
- Code Style: Match LEAN/QuantConnect conventions; include XML docs on public APIs.
- API Surface: Avoid expanding public surface area without clear necessity and tests.
- Security: Secrets configured via environment/secure stores; never committed or logged.
- Threading: Prefer CancellationToken, Task-based async; avoid Thread.Sleep on hot paths.
- Data Contracts: Mapping and symbol translation MUST be covered by unit tests.
- Third-Party Libraries: Only introduce well-maintained, permissive-licensed libraries
  with clear benefit; document them in PRs.

## Development Workflow & Quality Gates

- PR Requirements: CI green, tests added/updated, rationale explained, and risk noted.
- Reviews: At least one maintainer approval with explicit Constitution compliance check.
- CI Gates: build, unit tests, and integration smoke tests MUST pass.
- Changelog: Update release notes for user-visible changes; include migration notes for
  deprecations/breakers.
- Branch Hygiene: Temporary sync/* branches may be created by automation and cleaned up
  by maintenance workflows; never rely on them for long-lived work.

## Governance

- Scope: This Constitution governs engineering practices for the
  Lean.Brokerages.InteractiveBrokers repository and supersedes conflicting ad-hoc
  practices.
- Amendments: Proposed via PR updating this document and dependent templates. Version
  MUST be bumped per SemVer rules and Last Amended date updated.
- Compliance: Reviewers MUST block PRs that violate non-negotiable principles unless a
  governance exception is explicitly recorded with a remediation plan and versioned.
- Review Cadence: At least quarterly, or on upstream LEAN/IB breaking changes, run a
  compliance review across principles and quality gates.

**Version**: 1.0.0 | **Ratified**: 2025-09-28 | **Last Amended**: 2025-09-28
