param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe",
    [string]$Output = "Builds\Android\JewelMatch.apk"
)

$ErrorActionPreference = "Stop"
$ProjectPath = Resolve-Path "$PSScriptRoot\.."
$LogPath = Join-Path $ProjectPath "Logs\JewelMatchBuildAndroid.log"

& $UnityPath `
    -batchmode `
    -quit `
    -projectPath $ProjectPath `
    -executeMethod JewelMatchBuildTool.BuildAndroidFromCommandLine `
    -buildOutput $Output `
    -logFile $LogPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unity build failed. See log: $LogPath"
}

Write-Host "Build complete: $Output"
