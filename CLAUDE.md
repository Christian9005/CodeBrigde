# CodeBridge: notes for Claude Code

## Git and attribution
- Do **not** add `Co-Authored-By` trailers, "Generated with Claude Code" lines or any other AI attribution to commits, pull request descriptions or issue text. Commits are authored by the repository owner only.
- Commit and push only when asked. Work on a feature branch, never directly on `master`.

## Build and test
- Everything: `dotnet build CodeBridge.slnx -c Release`
- SDK tests: `dotnet test tests/CodeBridge.Core.Tests`
- Visual Studio extension tests (net472, WPF, run on STA threads): `dotnet test tests/CodeBridge.VisualStudio.Tests`
- Packages + VSIX: `pwsh build/pack.ps1 -Vsix`, then `pwsh tests/smoke_test.ps1` from the `tests` folder.
- ESP32 firmware: `cd firmware/esp32-bridge && python -m platformio run`, then copy `firmware.bin`, `bootloader.bin` and `partitions.bin` from `.pio/build/esp32` to `firmware/prebuilt/esp32`.

## Things that are easy to get wrong
- One version for everything: `Directory.Build.props`, `source.extension.vsixmanifest` and `Assets/toolbox-components.json` must match (a test checks it).
- Adding or changing a block touches: `BuiltInBlockCatalog`, `FlowRuntime`, `FlowCSharpGenerator`, `BlockHelp` + `BlockDemoView` (editor), tests, then regenerate `Assets/catalog.*.json` with `CodeBridge.FlowHost catalog --out src/CodeBridge.VisualStudio/Assets`.
- The firmware protocol version (`BridgeProtocol.EXPECTED_FIRMWARE_VERSION`, firmware `FIRMWARE_VERSION`, Uno `CODEBRIDGE_FIRMWARE_VERSION`) is compared by major.minor.
- The TCP/Wi-Fi port of the firmware requires a pairing token (`AUTH`, set over USB with `WTOK`); never reintroduce an unauthenticated network path.
- The VS extension runs on the UI thread: never let an exception escape an `async void` handler, and keep WPF code testable outside Visual Studio (see `VsTheme`, `NativeUi`).
- Source files mix LF and CRLF; keep a file's existing line endings.
