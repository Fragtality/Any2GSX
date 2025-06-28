# POST
# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyModule.ps1" $(Configuration) $(SolutionDir) $(ProjectDir)

if ($args[0] -eq "*Undefined*") {
	exit 0
}

if ($args[1] -eq "*Undefined*") {
	exit 0
}

$buildConfiguration = $args[0]
$pathBase = $args[1]
$pathProject = $args[2]
$packageName = "fragtality-commbus-module"
$moduleNameIn = "WASM-Module.wasm"
$moduleNameOut = $packageName + ".wasm"

$pathPackageProj20 = "MSFS2020-Package\PackageSources\WASM"
$pathPackageProj24 = "MSFS2024-Package\PackageSources\WASM"

$copySim = $false
$pathMSFS20 = "F:\MSFS2020\Packages\Community\" + $packageName + "\modules"
$pathMSFS24 = "F:\MSFS2020\Packages2024\Community\" + $packageName + "\modules"

try {	
	$pathModule = Join-Path $pathProject (Join-Path "bin" (Join-Path $buildConfiguration $moduleNameIn))
	Write-Host "Copy to Package Directory ..."
	#Package 2020
	$pathOutput = Join-Path (Join-Path $pathBase $pathPackageProj20) $moduleNameOut
	Copy-Item -Path $pathModule -Destination $pathOutput -Force | Out-Null
	
	#Package 2024
	$pathOutput = Join-Path (Join-Path $pathBase $pathPackageProj24) $moduleNameOut
	Copy-Item -Path $pathModule -Destination $pathOutput -Force | Out-Null
	
	#Direct Sim Output
	if ($copySim) {
		Write-Host "Copy to Sim ..."
		$pathOutput = Join-Path $pathMSFS20 $moduleNameOut
		Copy-Item -Path $pathModule -Destination $pathOutput -Force | Out-Null
		
		$pathOutput = Join-Path $pathMSFS24 $moduleNameOut
		Copy-Item -Path $pathModule -Destination $pathOutput -Force | Out-Null
	}
	Write-Host "SUCCESS: Copied WASM to Package!"
}
catch {
	Write-Host "FAILED: Exception in CopyModule.ps1!"
	exit -1
}