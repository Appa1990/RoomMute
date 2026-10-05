param([ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$installation = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$installation) { throw 'Install the Visual Studio C++ build tools to rebuild RNNoise.' }
$vcvars = Join-Path $installation 'VC/Auxiliary/Build/vcvarsall.bat'
$architecture = if ($Runtime -eq 'win-arm64') { 'amd64_arm64' } else { 'amd64' }
$root = $PSScriptRoot
$output = Join-Path $root $Runtime
$objects = Join-Path $root "../artifacts/native-$Runtime"
New-Item -ItemType Directory -Path $output,$objects -Force | Out-Null
$sources = 'denoise','rnn','pitch','kiss_fft','celt_lpc','nnet','nnet_default','parse_lpcnet_weights','rnnoise_data','rnnoise_tables'
$arguments = @('/nologo','/LD','/O2','/MT','/std:c11','/DRNNOISE_BUILD','/DDLL_EXPORT','/DWIN32','/D_CRT_SECURE_NO_WARNINGS',('/I"' + (Join-Path $root 'rnnoise/include') + '"'),('/I"' + (Join-Path $root 'rnnoise/src') + '"'),('/Fo"' + $objects + '/"'),('/Fe"' + (Join-Path $output 'rnnoise.dll') + '"'))
if ($Runtime -eq 'win-x64') { $arguments += '/D__SSE2__' } else { $arguments += '/Drestrict=__restrict' }
$arguments += $sources | ForEach-Object { '"' + (Join-Path $root "rnnoise/src/$_.c") + '"' }
$arguments += '/link','/Brepro',('/IMPLIB:"' + (Join-Path $objects 'rnnoise.lib') + '"')
$batch = Join-Path $env:TEMP ('roommute-native-' + [Guid]::NewGuid().ToString('N') + '.cmd')
@('@echo off', ('call "' + $vcvars + '" ' + $architecture + ' >nul'), 'if errorlevel 1 exit /b 1', ('pushd "' + $objects + '"'), ('cl ' + ($arguments -join ' ')), 'set result=%errorlevel%', 'popd', 'exit /b %result%') | Set-Content -LiteralPath $batch -Encoding ascii
try { & $env:ComSpec /d /c $batch; $result = $LASTEXITCODE }
finally { Remove-Item -LiteralPath $batch }
if ($result -ne 0) { throw 'RNNoise native build failed.' }
Write-Output "Built $Runtime RNNoise from vendored upstream sources."
