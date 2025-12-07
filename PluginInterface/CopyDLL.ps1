# POST
# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyDLL.ps1" $(Configuration) $(SolutionDir) $(ProjectDir) "Any2GSX-Plugins"

if ($args[0] -eq "*Undefined*") {
	exit 0
}

if ($args[1] -eq "*Undefined*") {
	exit 0
}

try {
	$buildConfiguration = $args[0]
	$pathSolution = $args[1]
	$pathProject = $args[2]
	$appFolderName = $args[3]

	$dllPath = Join-Path $pathProject (Join-Path (Join-Path "bin" $buildConfiguration) "\net10.0-windows10.0.17763.0\win-x64\PluginInterface.dll")
	$pathBase = (Resolve-Path (Join-Path $pathSolution "..")).Path
	
	$destPathPlugins = Join-Path $pathBase "Any2GSX\Any2GSX"
	$destPathDist = Join-Path $pathBase $appFolderName "dist\sdk"

	Copy-Item -Path $dllPath -Destination $destPathPlugins -Force | Out-Null
	Copy-Item -Path $dllPath -Destination $destPathDist -Force | Out-Null

	Write-Host "SUCCESS: Copy complete!"
	exit 0
}
catch {
	Write-Host "FAILED: Exception in CopyDLL.ps1!"
	cd $pathBase
	exit -1
}