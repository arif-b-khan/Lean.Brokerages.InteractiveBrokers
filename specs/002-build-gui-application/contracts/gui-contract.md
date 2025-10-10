# GUI Contract: Local API Surface

These endpoints describe the local API surface the GUI will use to interact with the Toolbox core.

1. GET /symbols?dataDir={path}
   - Returns: JSON array of available symbol identifiers found under the provided Lean data directory.

2. POST /load
   - Body: { "symbol": "AAPL", "resolution": "minute", "startDate": "2025-09-01", "endDate": "2025-09-30" }
   - Returns: { "snapshotId": "uuid", "records": [ {timestamp, open, high, low, close, volume}, ... ], "page": 1, "pageSize": 100 }

3. POST /download
   - Body: { "symbol": "AAPL", "resolution": "minute", "startDate": "2025-09-01", "endDate": "2025-09-30", "concurrency": 2, "retryPolicy": { "maxAttempts": 3 } }
   - Returns: { "jobId": "uuid" }

4. GET /download/{jobId}/status
   - Returns: { "jobId": "uuid", "status": "running|completed|failed", "progress": 0.42, "lastSuccessfulDate": "2025-09-05", "errors": [] }

Notes: These may be implemented as in-process method calls or exposed as a lightweight localhost HTTP API for decoupling.
