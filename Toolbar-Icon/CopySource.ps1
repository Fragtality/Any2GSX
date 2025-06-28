$pathSource = $pwd.Path + "\*"
#Write-Host $pathSource
cd "..\..\MSFS2020-Package\PackageSources\html_ui\"
$pathPackage = $pwd.Path
#Write-Host $pathPackage
Write-Host "Copy Source Files ..."
Copy-Item -Path $pathSource  -Exclude "package.json" -Destination $pathPackage -Force -Recurse | Out-Null