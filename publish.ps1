param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if ($Runtime -notin @('win-x64', 'win-arm64')) { throw 'Unterstützt: win-x64 oder win-arm64.' }
$packageName = "RoomMute-$Runtime-v1.4.1"
$outputDirectory = Join-Path $projectRoot "artifacts/$packageName"
dotnet publish (Join-Path $projectRoot 'RoomMute/RoomMute.csproj') -c Release -r $Runtime --self-contained true -o $outputDirectory -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $outputDirectory
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $outputDirectory
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY-NOTICES.txt') -Destination $outputDirectory
Compress-Archive -Path (Join-Path $outputDirectory '*') -DestinationPath (Join-Path $projectRoot "artifacts/$packageName.zip") -Force
Write-Output "Fertig: $outputDirectory"







