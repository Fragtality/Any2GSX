$msfsPackageDir = "F:\MSFS2020\Packages\Community"
####

$pathSource = (Join-Path $pwd.Path "InGamePanels\Any2GSX-Panel") + "\*"
$pathPackage = Join-Path $msfsPackageDir "fragtality-commbus-module\html_ui\InGamePanels\Any2GSX-Panel"

Write-Host "Copy to Sim ..."
Copy-Item -Path $pathSource -Destination $pathPackage -Force -Recurse | Out-Null