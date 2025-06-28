# AddPluginToRepo.ps1 <PLUGIN-FILE> <MANIFEST-PATH>

$fileName = $args[0]
$pathManifest = $args[1]

if (-not (Test-Path -Path "plugins")) {
	Write-Host "Not executed in dist Directory!"
	exit -1
}

if (-not $pathManifest -or -not (Test-Path -Path $pathManifest)) {
	Write-Host "Path to Manifest not found!"
	exit -1
}

$pathRepo = Join-Path $pwd.Path "plugins"
$pathRepoFile = Join-Path $pathRepo "plugin-repo.json"

try {
	$manifestData = Get-Content -Path $pathManifest -Raw | ConvertFrom-Json
	$repoData = Get-Content -Path $pathRepoFile -Raw | ConvertFrom-Json
	$operation = "added"
	$property = $repoData.PSobject.Properties | Where-Object { $_.name -eq $fileName }
	if ($property) {
		$operation = "updated"
		$property.Value = $manifestData
	}
	else {
		$repoData | Add-Member -MemberType NoteProperty -Name $fileName -Value ($manifestData)
	}
	$repoData | ConvertTo-Json -Depth 100 | Set-Content -Path $pathRepoFile
	
	Write-Host "Plugin $($manifestData.Id) v$($manifestData.VersionPlugin) $operation"
	exit 0
}
catch {
	Write-Host "EXCEPTION"
	exit -1
}