param(
  [string]$Configuration = 'Release',
  [string]$Output = ''
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnet = Join-Path $root '.tools/dotnet/dotnet.exe'
if (Test-Path $dotnet) {
  $env:DOTNET_CLI_HOME = Join-Path $root '.tools/home'
  $env:NUGET_PACKAGES = Join-Path $root '.tools/packages'
} else { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$project = Join-Path $root 'src/PizarrasPro/PizarrasPro.csproj'
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) { throw 'project version missing' }
if (-not $Output) { $Output = "artifacts/PizarrasPro-$version-portable-win-x64" }
$out = [IO.Path]::GetFullPath((Join-Path $root $Output))
if (-not $out.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be inside the project directory.' }
if (Test-Path $out) { throw 'Output already exists. Choose a new directory; existing portable settings will not be deleted.' }
if (Test-Path "$out.zip") { throw 'ZIP already exists. Choose a new output name.' }
& $dotnet restore $project -r win-x64 --locked-mode -p:UsedAvaloniaProducts=
if ($LASTEXITCODE -ne 0) { throw "restore failed ($LASTEXITCODE)" }
& $dotnet publish $project -c $Configuration -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:UsedAvaloniaProducts= -o $out
if ($LASTEXITCODE -ne 0) { throw "publish failed ($LASTEXITCODE)" }
$licenses = Join-Path $out 'licenses'; New-Item -ItemType Directory -Path $licenses -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs/licenses/SukiUI-6.1.1-LICENSE.txt') -Destination $licenses
& (Join-Path $PSScriptRoot 'collect-notices.ps1') -Output $out -Configuration $Configuration
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $out
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $out
Set-Content -LiteralPath (Join-Path $out 'README.txt') -Value "Pizarra Pro $version portable Windows x64`r`nExtraiga toda la carpeta y ejecute PizarrasPro.exe. Requiere Windows 10 2004 o posterior, x64.`r`nNo necesita instalar Python ni .NET. Use una carpeta con permiso de escritura.`r`nPara actualizar: cierre la aplicacion, conserve una copia de la carpeta anterior y copie preferencias.json y horario.json al nuevo portable.`r`nConsulte README.md para instrucciones, limites de WBH, operaciones de borrado y estado de la nube.`r`nLicencias: LICENSE, THIRD-PARTY-NOTICES.txt y licenses/." -Encoding UTF8
$exe = Join-Path $out 'PizarrasPro.exe'
if (-not (Test-Path $exe)) { throw 'published executable missing' }
Get-ChildItem -LiteralPath $out -Filter '*.pdb' | Remove-Item -Force
Compress-Archive -LiteralPath $out -DestinationPath "$out.zip"
Get-FileHash -Algorithm SHA256 -LiteralPath "$out.zip"
Write-Output "Published $exe"
