# AddProfileToRepo.ps1 <PROFILE-FILE> <NAME> <AIRCRAFT> <DESCRIPTION> <AUTHOR> <VPROFILE> <VAPP>

$pathProfileFile = $args[0]
$name = $args[1]
$aircraft = $args[2]
$description = $args[3]
$author = $args[4]
$versionProfile = $args[5]
$versionApp = $args[6]

if (-not (Test-Path -Path "profiles")) {
	Write-Host "Not executed in dist Directory!"
	exit -1
}

if (-not $pathProfileFile -or -not (Test-Path -Path $pathProfileFile)) {
	Write-Host "Path to Profile not found!"
	exit -1
}

$fileName = Split-Path $pathProfileFile -leaf
$pathRepo = Join-Path $pwd.Path "profiles"
$pathRepoFile = Join-Path $pathRepo "profile-repo.json"

try {
	$repoData = Get-Content -Path $pathRepoFile -Raw | ConvertFrom-Json
	$operation = "added"
	$property = $repoData.PSobject.Properties | Where-Object { $_.name -eq $fileName }
	$repoInfo = @{
		"Name" = $name
		"Aircraft" = $aircraft
		"Description" = $description
		"Author" = $author
		"VersionProfile" = $versionProfile
		"VersionApp" = $versionApp
	}
	if ($property) {
		$operation = "updated"
		$property.Value = $repoInfo
	}
	else {
		$repoData | Add-Member -MemberType NoteProperty -Name $fileName -Value ($repoInfo)
	}
	$repoData | ConvertTo-Json -Depth 100 | Set-Content -Path $pathRepoFile
	
	$test = Split-Path $pathProfileFile
	if ($test -ne $pathRepo) {
		Copy-Item -Path $pathProfileFile -Destination $pathRepo -Force | Out-Null
	}
	
	Write-Host "Profile '$($name)' v$($versionProfile) $operation"
	
	exit 0
}
catch {
	Write-Host "EXCEPTION"
	exit -1
}