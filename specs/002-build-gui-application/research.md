# Research: Cross-platform .NET GUI for Toolbox


Decision: Use AvaloniaUI (stable, cross-platform .NET UI)

Rationale:

- Avalonia supports Windows, macOS and Linux (including Ubuntu) with a single .NET codebase.

- .NET MAUI provides first-class support for Windows and macOS but Linux support is not official/stable; Avalonia is the better fit when Linux is required.

- The repository already targets .NET 9.0 in other projects; Avalonia works with .NET 7/8/9 and can be used without adding native platform SDKs beyond .NET runtime.

Alternatives considered:

- .NET MAUI: good for Windows/macOS/iOS/Android, but poor Linux support — rejected because user explicitly requires Ubuntu.

- Electron + ASP.NET backend: cross-platform but larger footprint (Node.js + Chromium), increases maintenance and binary size — rejected for simplicity and native look.

- Avalonia + CLI backend: chosen as best trade-off (native-ish UI, single .NET runtime).

Integration notes:

- Use a small local service layer inside the Toolbox project to run downloads and file I/O; GUI will call into this layer via direct method calls when launched via `dotnet run` in the toolbox project (no separate process required). Optionally expose a minimal local HTTP or named-pipe API for decoupling if needed later.

Security notes:

- Credentials are sensitive: persist encrypted values in a local JSON store (user-level). Research recommended using DPAPI on Windows, macOS Keychain and a simple file encryption with a user-specific key on Linux (or use a cross-platform library such as Azure.Core's DataProtection if available offline).
