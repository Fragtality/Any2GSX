$msBuildDir = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64"
$msfsSdkDir = "F:\MSFS2020\MSFS SDK"

#####

$currentDir = $pwd.Path
cd ..
$pathBase = $pwd.Path
$pathToolbar = Join-Path $pathBase "Toolbar-Icon\html_ui"
$binPackageTool = Join-Path $msfsSdkDir "Tools\bin\fspackagetool.exe"

Write-Host "npm run on Toolbar ..."
cd $pathToolbar
npm run build

Write-Host "msbuild for WASM Module ..."
cd $msBuildDir
.\msbuild.exe (Join-Path $pathBase "\Any2GSX.sln") /t:WASM-Module:rebuild /p:Configuration="Release" /p:BuildProjectReferences=false -verbosity:quiet | Out-Null

Write-Host "Create MSFS 2020 Package ..."
cd $currentDir
& $binPackageTool -rebuild -mirroring -nopause "fragtality-commbus-module.xml"

Write-Host "Create ZIP Archive ..."
$pathPublish = Join-Path $currentDir "Packages"
$zipPath = "Packages\fragtality-commbus-module.zip"
Remove-Item $zipPath -ErrorAction SilentlyContinue | Out-Null
& "C:\Program Files\7-Zip\7z.exe" a -tzip $zipPath ($pathPublish + "\*") | Out-Null
Copy-Item -Path $zipPath -Destination (Join-Path $pathBase "Installer\Payload\fragtality-commbus-module-2020.zip") -Force | Out-Null
$zipFile = Get-Item (Join-Path $pathBase "Installer\Payload\fragtality-commbus-module-2020.zip")
$zipFile.CreationTime = (Get-Date)

Write-Host "FINISHED!"
exit 0