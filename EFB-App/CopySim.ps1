$msfsPackageDir = "F:\MSFS2020\Packages2024\Community"
####

$pathSource = (Join-Path $pwd.Path "dist") + "\*"
$pathPackage = Join-Path $msfsPackageDir "fragtality-commbus-module\html_ui\efb_ui\efb_apps\Any2GsxApp"

Write-Host "Copy to Sim ..."
Copy-Item -Path $pathSource -Destination $pathPackage -Force -Recurse | Out-Null