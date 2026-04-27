$ErrorActionPreference = "Stop"

$ProjectDir = Join-Path $PSScriptRoot "ScheduleCollege.Web"
$OutDir = Join-Path $PSScriptRoot "publish-single"

if (Test-Path $OutDir) {
    Remove-Item $OutDir -Recurse -Force
}

Write-Host "Publishing ScheduleCollege as one EXE..." -ForegroundColor Cyan

dotnet publish (Join-Path $ProjectDir "ScheduleCollege.Web.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:RazorCompileOnBuild=true `
    -p:RazorCompileOnPublish=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $OutDir

$ExePath = Join-Path $OutDir "ScheduleCollege.Web.exe"
if (-not (Test-Path $ExePath)) {
    throw "EXE was not created: $ExePath"
}

# Оставляем рядом только EXE. Runtime-файлы и CSS создаются в %LOCALAPPDATA%\ScheduleCollege.
Get-ChildItem $OutDir | Where-Object { $_.Name -ne "ScheduleCollege.Web.exe" } | Remove-Item -Recurse -Force

Write-Host "Done:" -ForegroundColor Green
Write-Host $ExePath
Write-Host "Runtime data folder: $env:LOCALAPPDATA\ScheduleCollege"
