param(
    [string]$VsixPath = "..\src\CodeBridge.VisualStudio\bin\Release\net472\CodeBridge.VisualStudio.vsix"
)

Write-Host "Running CodeBridge VSIX Smoke Tests..."

if (-not (Test-Path $VsixPath)) {
    Write-Error "VSIX not found at $VsixPath. Build the project first."
    exit 1
}

$tempDir = Join-Path $env:TEMP "CodeBridgeVsixSmokeTest_$(New-Guid)"
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Write-Host "Extracting VSIX..."
    Expand-Archive -Path $VsixPath -DestinationPath $tempDir -Force

    $expectedFiles = @(
        "CodeBridge.VisualStudio.dll",
        "extension.vsixmanifest",
        "ProjectTemplates\CodeBridgeWinFormsApp\CodeBridgeWinFormsApp.vstemplate",
        "ProjectTemplates\CodeBridgeWinFormsApp\ProjectTemplate.csproj",
        "FlowHost.zip",
        "Assets\catalog.esp32-devkit.json",
        "Assets\catalog.arduino-uno.json"
    )

    $failed = $false

    foreach ($file in $expectedFiles) {
        $fullPath = Join-Path $tempDir $file
        if (-not (Test-Path $fullPath)) {
            Write-Error "Missing expected file: $file"
            $failed = $true
        } else {
            Write-Host "Found expected file: $file" -ForegroundColor Green
        }
    }

    # Check for NuGet package
    $packagesDir = Join-Path $tempDir "Packages"
    if (Test-Path $packagesDir) {
        $nupkgs = Get-ChildItem -Path $packagesDir -Filter "*.nupkg"
        if ($nupkgs.Count -eq 0) {
            Write-Error "No .nupkg files found in Packages directory."
            $failed = $true
        } else {
            foreach ($nupkg in $nupkgs) {
                Write-Host "Found NuGet package: $($nupkg.Name)" -ForegroundColor Green
            }
        }
    } else {
        Write-Error "Missing Packages directory."
        $failed = $true
    }

    if ($failed) {
        Write-Error "Smoke test FAILED."
        exit 1
    } else {
        Write-Host "Smoke test PASSED!" -ForegroundColor Green
        exit 0
    }
} finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}
