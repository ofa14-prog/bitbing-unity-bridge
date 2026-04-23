# Changelog

All notable changes to `com.bitbing.unity-bridge` are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- Renamed all asmdefs and namespaces from `AIGameDev.*` → `BitBing.*`.
- Settings menu relocated to **Project Settings → BitBing Unity Bridge**.
- Editor window menu relocated to **Window → BitBing → Agent Panel**.
- Default Named Pipe name: `aigamedev-unity` → `bitbing-unity`.
- Package now installs via `?path=/com.bitbing.unity-bridge` git URL
  (monorepo subfolder layout, matching COPLAY's `unity-mcp` convention).

### Added
- Package-root `README.md`, `CHANGELOG.md`, `LICENSE.md`.

## [0.1.0] - 2026-01-01

### Added
- Initial UPM package scaffold.
- TCP socket bridge (port 57432) and MCP listener.
- UI Toolkit Agent Panel editor window.
- Command set: `create_gameobject`, `delete_gameobject`, `add_component`,
  `write_script`, `compile_scripts`, `create_scene`, `open_scene`,
  `enter_play_mode`, `exit_play_mode`, `take_screenshot`, `run_tests`.
- `BridgeSettings` ScriptableObject + Project Settings provider.
