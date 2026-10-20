$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $root 'src\PeakSentinel'
$version = (Select-String -Path (Join-Path $project 'SentinelPlugin.cs') -Pattern 'Version = "([^"]+)"').Matches[0].Groups[1].Value
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'stage'
$tar = Join-Path $env:SystemRoot 'System32\tar.exe'
$utf8 = New-Object System.Text.UTF8Encoding $false

dotnet build $project -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage 'plugins') | Out-Null
Copy-Item (Join-Path $project 'bin\Release\netstandard2.1\PeakSentinel.dll') (Join-Path $stage 'plugins')
Copy-Item (Join-Path $PSScriptRoot 'thunderstore\icon.png') $stage
Copy-Item (Join-Path $PSScriptRoot 'thunderstore\README.md') $stage
$manifest = (Get-Content (Join-Path $PSScriptRoot 'thunderstore\manifest.json') -Raw -Encoding UTF8).Replace('{VERSION}', $version)
[System.IO.File]::WriteAllText((Join-Path $stage 'manifest.json'), $manifest, $utf8)

$zip = Join-Path $dist "PeakSentinel-$version-thunderstore.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
# Windows tar writes standard forward-slash zip entries, Compress-Archive in PowerShell 5.1 does not
& $tar -a -c -f $zip -C $stage @(Get-ChildItem $stage -Force | ForEach-Object Name)
if ($LASTEXITCODE -ne 0) { throw 'Packing failed' }
Remove-Item $stage -Recurse -Force
Get-Item $zip | ForEach-Object { Write-Host ("Packed {0}  {1:N0} KB" -f $_.FullName, ($_.Length / 1KB)) }