param(
    [Parameter(Mandatory=$true)][string]$OriginalMsix,
    [string]$Destination = (Join-Path $PSScriptRoot 'build')
)
$ErrorActionPreference = 'Stop'
# Run with PowerShell 7. The original app remains required; no third-party binaries are included in the sources.
$OriginalMsix=(Resolve-Path -LiteralPath $OriginalMsix).Path
$Destination=[IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$stage=Join-Path $Destination ('package-'+[guid]::NewGuid().ToString('N'))
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($OriginalMsix,$stage)
$manifestPath=Join-Path $stage 'AppxManifest.xml'
$manifest=[xml](Get-Content -LiteralPath $manifestPath -Raw)
if($manifest.Package.Identity.Name -ne 'NeverFinishAnything.HedgeModManagerUwp' -or $manifest.Package.Identity.Version -ne '0.1.1.0') {throw 'Este patch requer o pacote original HedgeModManager UWP 0.1.1.0.'}
$manifest.Package.Identity.Version='0.1.7.0'
$resourceContainer=$manifest.DocumentElement.SelectSingleNode("*[local-name()='Resources']")
$resourceContainer.RemoveAll()
foreach($language in @('en-US','es-ES','pt-BR','it-IT','fr-FR')) {
    $resource=$manifest.CreateElement('Resource',$manifest.DocumentElement.NamespaceURI)
    $resource.SetAttribute('Language',$language)
    [void]$resourceContainer.AppendChild($resource)
}
$manifest.Save($manifestPath)
$depProject=Join-Path $Destination 'References.csproj'
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NETCore.UniversalWindowsPlatform" Version="6.2.14"/></ItemGroup></Project>' | Set-Content -LiteralPath $depProject
& dotnet restore $depProject
if($LASTEXITCODE -ne 0){throw 'Falha ao restaurar referências UWP.'}
$sdkEntry=(& dotnet --list-sdks | Select-Object -Last 1)
if(!$sdkEntry){throw 'Instale o SDK .NET 6 ou mais recente.'}
$sdkVersion=$sdkEntry.Split(' ')[0];$sdkRoot=($sdkEntry -split '\[')[1].TrimEnd(']')
$csc=Join-Path $sdkRoot "$sdkVersion\Roslyn\bincore\csc.dll"
$kits=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10'
$metadata=Get-ChildItem (Join-Path $kits 'UnionMetadata') -Filter Windows.winmd -Recurse | Where-Object FullName -Match '10\.0\.19041\.0\\Windows.winmd$' | Select-Object -First 1
if(!$metadata){throw 'Instale o Windows SDK 10.0.19041.0.'}
$refs=Join-Path $env:USERPROFILE '.nuget\packages\microsoft.netcore.universalwindowsplatform\6.2.14\ref\uap10.0.15138'
if($env:NUGET_PACKAGES){$refs=Join-Path $env:NUGET_PACKAGES 'microsoft.netcore.universalwindowsplatform\6.2.14\ref\uap10.0.15138'}
$helper=Join-Path $stage 'HedgeModManager.Improved.dll'
$originalExe=Join-Path $stage 'entrypoint\HedgeModManager.Uwp.exe'
$rsp=@('/nostdlib+','/target:library','/langversion:10','/optimize+',('/out:"'+$helper+'"'))
$rsp+=Get-ChildItem -LiteralPath $refs -Filter '*.dll' | ForEach-Object {'/reference:"'+$_.FullName+'"'}
$rsp+='/reference:"'+$metadata.FullName+'"'
$rsp+='/reference:"'+$originalExe+'"'
$rsp+=Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object {'"'+$_.FullName+'"'}
$response=Join-Path $Destination 'compile.rsp';$rsp | Set-Content -LiteralPath $response
& dotnet $csc "@$response"
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar a interface UWP.'}
$patched=Join-Path $Destination 'patched.exe'
& dotnet run --project (Join-Path $PSScriptRoot 'Patch\Patch.csproj') --configuration Release -- $originalExe $helper $patched
if($LASTEXITCODE -ne 0){throw 'Falha ao aplicar o patch.'}
Copy-Item -LiteralPath $patched -Destination $originalExe -Force
foreach($relative in @('AppxSignature.p7x','AppxBlockMap.xml','[Content_Types].xml','AppxMetadata\CodeIntegrity.cat')) {
    $candidate=Join-Path $stage $relative
    if(Test-Path -LiteralPath $candidate){Remove-Item -LiteralPath $candidate}
}
$sdkBin=Join-Path $kits 'bin\10.0.19041.0\x64'
$output=Join-Path $Destination 'HedgeModManager.Uwp_0.1.7.0_x64_Improved.msix'
& (Join-Path $sdkBin 'makeappx.exe') pack /d $stage /p $output /o
if($LASTEXITCODE -ne 0){throw 'Falha ao empacotar o MSIX.'}
Write-Output "Pacote gerado: $output"
Write-Output 'O pacote reconstruído precisa ser assinado com um certificado de desenvolvimento CN=HedgeModManagerUwp antes da instalação.'
