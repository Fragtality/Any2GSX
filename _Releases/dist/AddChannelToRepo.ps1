# AddChannelToRepo.ps1 <CHANNEL-PATH>

$pathChannnel = $args[0]

if (-not (Test-Path -Path "channel")) {
	Write-Host "Not executed in dist Directory!"
	exit -1
}

if (-not $pathChannnel -or -not (Test-Path -Path $pathChannnel)) {
	Write-Host "Path to Channel not found!"
	exit -1
}

$fileName = Split-Path $pathChannnel -leaf
$pathRepo = Join-Path $pwd.Path "channel"
$pathRepoFile = Join-Path $pathRepo "channel-repo.json"

try {
	$manifestData = Get-Content -Path $pathChannnel -Raw | ConvertFrom-Json
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
	Copy-Item -Path $pathChannnel -Destination $pathRepo -Force | Out-Null
	
	Write-Host "Channel $($manifestData.Id) v$($manifestData.VersionChannel) $operation"
	exit 0
}
catch {
	Write-Host "EXCEPTION"
	exit -1
}