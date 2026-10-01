# Security Policy

## Supported versions

Only the latest released version receives security fixes.

## Reporting a vulnerability

Please do **not** open a public issue. Use GitHub's private reporting:
<https://github.com/Christian9005/CodeBrigde/security/advisories/new>

Include the affected version, reproduction steps and impact. You will get an answer within 7 days.

## What CodeBridge does on your machine

- Downloads `esptool` from GitHub Releases over HTTPS the first time you flash an ESP32, and verifies a pinned SHA-256 before using it.
  Files are stored under `%LOCALAPPDATA%\CodeBridge\tools`.
- Runs `esptool`, `arduino-cli` and `dotnet` as child processes (flash and package install commands only).
- Does not collect telemetry and does not send data anywhere.
