# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.3] - 2025-12-28

### Added
- **Docs:** Added a demo screenshot to `README.md` to showcase the application menu.

### Changed
- **Docs:** Refined `README.md` to use GitHub-style alerts (WARNING/TIP) for better visibility in the Troubleshooting section.
- **Docs:** Generalized the description to "super lightweight" in `README.md` for improved accuracy.

## [1.0.2] - 2025-12-28

### Changed
- **Defaults:** Changed default auto-delete time to **"No auto-delete"** (0 minutes) on first launch to prevent accidental deletion.
- **Icon:** Improved icon reliability by embedding `icon.png` directly into the executable (fixes blurry icon on some Single-File builds).
- **Code:** Translated all source code comments to English.
- **Docs:** Updated README with explicit Screenshot Folder selection instructions.

## [1.0.1] - 2025-12-28

### Changed
- **Code:** Translated all source code comments from Vietnamese to English.
- **Docs:** Refined README structure and language.

## [1.0.0] - 2025-12-28

### Added
- **Core:** Auto-delete functionality for screenshots based on user-defined timer (`15m`, `30m`, `1h`, `24h`).
- **Core:** Custom Screenshots folder selection using user-friendly dialog.
- **System:** System Tray integration with context menu.
- **System:** "Run at Windows Startup" option via Registry integration.
- **UI:** Full English localization.
- **UI:** High-contrast, transparent tray icon.
- **Docs:** Comprehensive README with installation and usage guide.

### Fixed
- Fixed issue where default Windows Screenshots path might not be detected correctly on some systems (using `SHGetKnownFolderPath`).
- Eliminated potential conflicts with other file types by strictly tagging files with `_AUTODEL_`.
