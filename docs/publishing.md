# Publishing checklist

Everything is driven by one version: `<Version>` in `Directory.Build.props`. The NuGet packages, the VSIX manifest and
`toolbox-components.json` must match it (a unit test enforces this).

## One-time setup

1. **nuget.org**: create an account, reserve the `CodeBridge.` ID prefix, create an API key scoped to `CodeBridge.*`, store it as the GitHub
   secret `NUGET_API_KEY`.
2. **Visual Studio Marketplace**: create a publisher at <https://marketplace.visualstudio.com/manage>. Put its ID in `vs-publish.json`
   (`publisher`) and in `source.extension.vsixmanifest` (`Identity/@Publisher`). Create an Azure DevOps PAT with scope
   *Marketplace > Manage* and store it as `VS_MARKETPLACE_PAT`.
3. Optional: sign the VSIX (`tools/CodeBridge.VisualStudio.Signing`). Not required by the Marketplace.

## Each release

1. Bump `<Version>` (Directory.Build.props), `Identity/@Version` (vsixmanifest), `manifestVersion` and `package.version`
   (`Assets/toolbox-components.json`). Update `CHANGELOG.md`.
2. If the firmware changed: bump `FIRMWARE_VERSION`/`EXPECTED_FIRMWARE_VERSION`, rebuild with `pio run -d firmware/esp32-bridge` and copy
   `.pio/build/esp32/{firmware,bootloader,partitions}.bin` (and `boot_app0.bin`) to `firmware/prebuilt/esp32/`.
3. `./build/pack.ps1 -Vsix` then `dotnet test CodeBridge.slnx -c Release` and `./tests/smoke_test.ps1 -VsixPath artifacts/vsix/CodeBridge.VisualStudio.vsix`.
4. **Manual test on a clean machine / VS Experimental Instance**: install the VSIX, create the project, *Tools > CodeBridge Setup > Install*,
   add the starter form, build, flash a real ESP32, run Blink, open a `.cbflow` file in Light and Dark themes.
5. `git tag vX.Y.Z && git push --tags`. The `Release` workflow pushes NuGet packages, publishes the VSIX and creates the GitHub release.
