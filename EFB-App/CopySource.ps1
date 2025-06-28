$pathSource = (Join-Path $pwd.Path "dist") + "\*"
#Write-Host $pathSource
cd "..\..\MSFS2024-Package\PackageSources\Any2GsxApp\"
$pathPackage = $pwd.Path
#Write-Host $pathPackage
Write-Host "Copy Source Files ..."
Copy-Item -Path $pathSource -Destination $pathPackage -Force -Recurse | Out-Null