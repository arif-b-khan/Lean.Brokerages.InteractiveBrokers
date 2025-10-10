# Feature Specification: Toolbox Cross-Platform GUI for IB Configuration and Data Review

**Feature Branch**: `002-build-gui-application`  
**Created**: 2025-10-02  
**Status**: Draft  
**Input**: User description:

```text
build gui application which will be cross platform in toolbox project. This application will allow me to select all these options
IB_USERNAME=your_ib_username
IB_PASSWORD=your_ib_password
IB_ACCOUNT=U1234567

# Optional overrides
IB_GATEWAY_DIR=/Users/your-user/Jts
IB_VERSION=latest
IB_TRADING_MODE=paper
IB_AUTOMATER_EXPORT_LOGS=false
GATEWAY_HOST=127.0.0.1
GATEWAY_PORT=7497
DATA_DIR=/Users/{username}/github/Lean/Data
This is basically application which will also allow me to view the data stored in lean path.
I should be able to view the data loaded in a grid.
- I should be able to select the symbol to load the data for that symbol.
- Don't create separate project build guid application in toolbox project
```

## User Scenarios & Testing *(mandatory)*

### Primary User Story

As a quant developer using the Interactive Brokers Toolbox utilities, I want a cross-platform graphical experience embedded in the existing toolbox so that I can enter IB connection settings, choose a Lean data directory, and inspect stored market data without editing environment files manually.

### Acceptance Scenarios

1. **Given** the toolbox GUI is launched, **When** the user enters IB credentials and overrides in the provided fields and saves them, **Then** the configuration is validated for completeness and persisted for the current session locally in json file this file will never be checked in
2. **Given** a Lean data directory is selected, **When** the user selects a symbol and timeframe, **Then** the application loads the corresponding Lean-formatted data file(s) and displays the records in a tabular grid ordered by timestamp.
3. **Given** the user has loaded a dataset into the grid, **When** the dataset is large, **Then** the grid supports paging or scrolling without freezing and the user can filter or search by date/time slice.

### Edge Cases

- What happens when the specified Lean data directory does not exist or lacks required permissions? The system should prompt the user to re-select a valid directory and explain the error.
- How does the application handle symbols that have no data files within the directory? It should display a "no data found" message instead of leaving the grid blank without context.
- Password should be encrypt and saved to simple json file which will never by checked in. On UI password should be masked but user should have the ability to veiw th masked password

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The GUI MUST be launchable from within the existing toolbox project without creating a separate application artifact.
- **FR-002**: The interface MUST present input fields for IB credentials (`IB_USERNAME`, `IB_PASSWORD`, `IB_ACCOUNT`) and optional overrides (`IB_GATEWAY_DIR`, `IB_VERSION`, `IB_TRADING_MODE`, `IB_AUTOMATER_EXPORT_LOGS`, `GATEWAY_HOST`, `GATEWAY_PORT`, `DATA_DIR`).
- **FR-003**: Users MUST be able to update the Lean data directory path through the UI and see the currently active directory.
- **FR-004**: The system MUST validate required fields and surface clear error messages for missing or obviously invalid values (e.g., non-numeric port, empty username).
- **FR-005**: The GUI MUST provide a control to choose a symbol from available Lean data (e.g., via search, dropdown, or file detection) and allow the user to request data loading for that symbol.
- **FR-006**: Upon symbol selection, the system MUST load Lean-formatted market data (at least minute resolution per prompt) into a grid with columns for timestamp, open, high, low, close, and volume.
- **FR-007**: The grid MUST support viewing large datasets through pagination, virtualized scrolling, or similar so that monthly data does not degrade responsiveness beyond acceptable UI latency (target under 2 seconds to render initial view).
- **FR-008**: Users MUST be able to refresh the grid after updating configuration or selecting a different symbol without restarting the GUI.
- **FR-009**: The system MUST preserve session changes long enough to run downstream toolbox commands and provide an option to export or sync values back to `.env` while warning users before overwriting existing secrets.
- **FR-010**: The GUI MUST communicate status and errors (e.g., loading progress, missing files, parsing failures) with non-blocking notifications.
- **FR-011**: The solution MUST operate consistently on macOS, Windows, and Linux environments supported by the toolbox, using a single codebase.
- **FR-012**: The system MUST prevent accidental logging or display of sensitive values such as `IB_PASSWORD` in plain text outside masked input fields.

- **FR-013**: The GUI MUST support full Interactive Brokers integration: it shall be able to connect to IB Gateway/TWS, initiate and schedule multi-day historical data downloads for selected symbols (with batching and retries), and manage the IB session lifecycle (connect, disconnect, reconnect).

## Clarifications

### Session 2025-10-02

- Q: Should the GUI trigger actual data downloads from IB or is it limited to viewing already stored Lean data? → A: C (Full IB integration: GUI can connect, schedule, and perform multi-day downloads including batching and retry, and manage the IB session)

### Non-Functional Requirements (related)

- **NFR-001**: When performing multi-day downloads the system MUST support configurable concurrency and retry policies and expose progress to the user; default concurrency should avoid IB request throttling.

### Edge Cases (additional)

- When IB requests fail or the gateway disconnects during a multi-day download, the GUI MUST persist the download progress and allow resume/retry from the last successful date; it should also surface actionable error messages.

### Key Entities *(include if feature involves data)*

- **Brokerage Configuration**: Represents the collection of user-provided values needed to connect to Interactive Brokers (credentials and overrides). Attributes include username, password (masked), account ID, gateway host/port, gateway directory, trading mode, automater options, and Lean data directory path. Relationships: used to populate environment variables for toolbox scripts.
- **Lean Data Snapshot**: Represents a set of market data records loaded from Lean-formatted files for a single symbol and resolution. Attributes include symbol identifier, resolution, date range, metadata about source files, and the list of bar records displayed in the grid.
- **Bar Record**: Represents an individual market data entry with timestamp, open, high, low, close, volume, and source file reference. Relationship: belongs to a Lean Data Snapshot.

## Review & Acceptance Checklist

### Content Quality

- [ ] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness

- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous  
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist passed

