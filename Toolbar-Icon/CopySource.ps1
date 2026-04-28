$origin = $pwd.Path
$pathSource = $pwd.Path + "\*"

cd "..\..\MSFS2020-Package\PackageSources\html_ui\"
$pathPackage = $pwd.Path
cd $origin
cd "..\..\MSFS2024-Package\PackageSources\html_ui\"
$pathPackage24 = $pwd.Path
cd $origin

Write-Host "Copy Source Files ..."
Copy-Item -Path $pathSource  -Exclude "package.json" -Destination $pathPackage -Force -Recurse | Out-Null
Copy-Item -Path $pathSource  -Exclude "package.json" -Destination $pathPackage24 -Force -Recurse | Out-Null