# Validation for 0.1.0

Checked on Windows 11 x64 with Windows PowerShell 5.1 and the official `OpenAI.Codex` package version `26.1002.7124.0`.

The package test completed 23 checks:

- PowerShell script parsing and C# launcher compilation.
- Native Windows extraction of all seven icon sizes, from 16 to 256 pixels.
- Installation to a path containing spaces and creation of separate application/family configuration values.
- Read-only helper validation and matching of a visible Codex package process.
- Rejection of invalid, non-Codex configuration.
- Shortcut AppUserModelID assignment and native shell icon extraction.
- Upgrade backup creation.
- Removal of the owned launcher while retaining unrelated files and upgrade backups.

The preceding local implementation was visually checked on the same machine: both the desktop shortcut and the running taskbar button displayed the enlarged transparent cloud icon. The portable version retains that window-icon code and replaces path-based process matching with package-family matching.

Not verified on another user's computer, ARM64, Windows 10, an unpackaged Codex install, or across a future application update. Test scripts do not change live window icons; `--check` only validates configuration, icon loading, and package matching.
