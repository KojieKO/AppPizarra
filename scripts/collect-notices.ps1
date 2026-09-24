param([Parameter(Mandatory=$true)][string]$Output, [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assets = Get-Content (Join-Path $root 'src/PizarrasPro/obj/project.assets.json') -Raw | ConvertFrom-Json
$cache = ($assets.packageFolders.PSObject.Properties | Select-Object -First 1).Name
$licenses = Join-Path $Output 'licenses'
New-Item -ItemType Directory -Path $licenses -Force | Out-Null
$notice = [Collections.Generic.List[string]]::new()
$notice.Add('Pizarra Pro - Third-party dependency notices')
$notice.Add('Inventory from the restored NuGet assets, including build and platform dependencies. Each component retains its own license.')
$mit = Get-Content (Join-Path $root 'docs/licenses/SukiUI-6.1.1-LICENSE.txt') -Raw
$mitBody = $mit.Substring($mit.IndexOf('Permission is hereby granted'))
foreach ($library in ($assets.libraries.PSObject.Properties | Sort-Object Name)) {
  if ($library.Value.type -ne 'package') { continue }
  $directory = Join-Path $cache $library.Value.path
  $nuspec = Get-ChildItem -LiteralPath $directory -Filter '*.nuspec' | Select-Object -First 1
  if (-not $nuspec) { throw "Missing package metadata: $($library.Name)" }
  $xml = [xml](Get-Content -LiteralPath $nuspec.FullName -Raw)
  $meta = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
  $license = $meta.SelectSingleNode('*[local-name()="license"]')
  $copyright = $meta.SelectSingleNode('*[local-name()="copyright"]')
  $url = $meta.SelectSingleNode('*[local-name()="licenseUrl"]')
  $name = $library.Name.Replace('/', '-')
  $destination = Join-Path $licenses $name
  New-Item -ItemType Directory -Path $destination -Force | Out-Null
  Copy-Item -LiteralPath $nuspec.FullName -Destination $destination
  $notice.Add("`r`n$($library.Name)`r`n$($copyright.InnerText)`r`nLicense: $($license.InnerText)`r`n$($url.InnerText)")
  $files = Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match '(?i)license|notice|copying' }
  foreach ($file in $files) { Copy-Item -LiteralPath $file.FullName -Destination $destination }
  if ($license -and $license.GetAttribute('type') -eq 'file') {
    $licensePath = Join-Path $directory $license.InnerText
    if (-not (Test-Path -LiteralPath $licensePath)) { throw "License missing: $licensePath" }
    Copy-Item -LiteralPath $licensePath -Destination $destination -Force
  } elseif ($license.InnerText -eq 'MIT') {
    Set-Content -LiteralPath (Join-Path $destination 'MIT-LICENSE.txt') -Value ("MIT License`r`n$($copyright.InnerText)`r`n`r`n" + $mitBody) -Encoding UTF8
  }
}
$project = [xml](Get-Content (Join-Path $root 'src/PizarrasPro/PizarrasPro.csproj') -Raw)
$framework = $project.Project.PropertyGroup.TargetFramework | Select-Object -First 1
$runtimeConfig = Get-Content (Join-Path $root "src/PizarrasPro/bin/$Configuration/$framework/win-x64/PizarrasPro.runtimeconfig.json") -Raw | ConvertFrom-Json
$runtime = $runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App'
if (-not $runtime) { throw 'Self-contained runtime metadata missing.' }
$runtimeDirectory = Join-Path $cache "microsoft.netcore.app.runtime.win-x64/$($runtime.version)"
$runtimeDestination = Join-Path $licenses "Microsoft.NETCore.App-$($runtime.version)"
New-Item -ItemType Directory -Path $runtimeDestination -Force | Out-Null
foreach ($file in @('LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT')) {
  Copy-Item -LiteralPath (Join-Path $runtimeDirectory $file) -Destination $runtimeDestination
}
$notice.Add("`r`nMicrosoft.NETCore.App $($runtime.version): see licenses/Microsoft.NETCore.App-$($runtime.version)/")
Set-Content -LiteralPath (Join-Path $Output 'THIRD-PARTY-NOTICES.txt') -Value $notice -Encoding UTF8
