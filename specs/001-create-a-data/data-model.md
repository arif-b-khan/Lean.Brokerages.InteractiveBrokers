# Data Model: IB Data Download ToolBox

## Entities

- DownloadRequest
  - symbol: string (e.g., AAPL)
  - securityType: enum (Equity, Futures)
  - exchange: string (e.g., SMART, GLOBEX)
  - currency: string (e.g., USD)
  - resolution: enum (Tick, Second, Minute, Hour, Daily)
  - from: DateTime
  - to: DateTime
  - dataDir: string (output root)
  - backoff: { baseMs: int, maxMs: int, maxRetries: int }

- DownloadResult
  - success: bool
  - files: string[] (relative paths under dataDir)
  - warnings: string[]
  - error: string | null

- CredentialSource
  - env: { IB_USERNAME, IB_PASSWORD, IB_ACCOUNT } (do not log values)
  - configPath: string | null (JSON file with equivalent keys)

- OutputLayout
  - methods:
    - GetPath(request): string (folder path per LEAN conventions)
    - GetFilename(request, date): string
    - SerializeBar(resolution, bar): string (CSV line)

## Validation Rules

- from <= to; else error
- symbol non-empty; resolution supported for securityType
- dataDir exists and writable; else fail fast
- credentials must be available from env or config; never both empty

## Relationships

- DownloadRequest -> DownloadResult (1:1 per run)
- CredentialSource used by session creation
- OutputLayout used by writer service
