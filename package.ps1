#!/usr/bin/env pwsh
# Copyright (c) 2023 Roger Brown.
# Licensed under the MIT License.

param($OutDir)

$ModuleName = 'PowerShellDataFile'
$CompanyName = 'rhubarb-geek-nz'

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$DSC = [System.IO.Path]::DirectorySeparatorChar

function Get-SingleNodeValue([System.Xml.XmlDocument]$doc,[string]$path)
{
    return $doc.SelectSingleNode($path).FirstChild.Value
}

trap
{
	throw $PSItem
}

$xmlDoc = [System.Xml.XmlDocument](Get-Content "$ModuleName.csproj")

$ModuleId = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/PackageId'
$Version = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/Version'
$ProjectUri = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/PackageProjectUrl'
$Description = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/Description'
$Author = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/Authors'
$Copyright = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/Copyright'
$AssemblyName = Get-SingleNodeValue $xmlDoc '/Project/PropertyGroup/AssemblyName'

$ModulePath = ( $OutDir + $ModuleId )

$null = New-Item -ItemType Directory -Path $ModulePath

Get-ChildItem -LiteralPath $OutDir -Filter '*.dll' | ForEach-Object {
	$_ | Copy-Item -Destination $ModulePath
}

Copy-Item -LiteralPath 'README.md' -Destination $ModulePath

$moduleSettings = @{
	Path = "$ModuleId.psd1"
	RootModule = "$AssemblyName.dll"
	ModuleVersion = $Version
	Guid = '73363885-3c20-4b43-bdaa-027b4e6217ea'
	Author = $Author
	CompanyName = $CompanyName
	Copyright = $Copyright
	Description = $Description
	FunctionsToExport = @()
	CmdletsToExport = @('Export-PowerShellDataFile')
	VariablesToExport = '*'
	AliasesToExport = @()
	ProjectUri = $ProjectUri
}

New-ModuleManifest @moduleSettings

Import-PowerShellDataFile -LiteralPath "$ModuleId.psd1" | Export-PowerShellDataFile | Set-Content -LiteralPath "$ModulePath$DSC$ModuleId.psd1"

Remove-Item -LiteralPath "$ModuleId.psd1"
