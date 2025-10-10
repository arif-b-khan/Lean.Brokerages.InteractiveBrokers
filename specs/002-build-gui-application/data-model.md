# Data Model: GUI for Toolbox

Entities:

- BrokerageConfiguration
  - id: string (uuid)
  - username: string
  - password_encrypted: string
  - account: string
  - gateway_host: string
  - gateway_port: int
  - gateway_dir: string
  - version: string
  - trading_mode: string
  - automater_export_logs: bool
  - data_dir: string
  - created_at: datetime
  - updated_at: datetime

- LeanDataSnapshot
  - id: string (uuid)
  - symbol: string
  - resolution: string
  - start_date: date
  - end_date: date
  - source_files: list of strings
  - record_count: int
  - loaded_at: datetime

- BarRecord
  - timestamp: datetime
  - open: decimal
  - high: decimal
  - low: decimal
  - close: decimal
  - volume: long
  - source_file: string

Notes:

- Store configurations in a user-level JSON file encrypted per OS best practices. Keep data snapshots as ephemeral views referencing files on disk (do not copy large files into app storage).
