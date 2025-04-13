param (
    [Parameter(Mandatory=$true)]
    [string]$OutDir,

    [Parameter(Mandatory=$true)]
    [string]$AssemblyName
)


Write-Host "Deploy script begin"

# Set directories
$SteamDir = "C:/Program Files (x86)/Steam/steamapps/common"
$ServerDir = "$SteamDir/Valheim dedicated server/BepInEx/plugins"
$ClientDir = "$SteamDir/Valheim/BepInEx/plugins"
$ArtifactsSpec = "./$($OutDir)$($AssemblyName).*"
$ReleaseStaging = "./release_staging"
$ZipStaging = "$ReleaseStaging/zip_staging"
$ArtifactDirForZip = "$ZipStaging/BepInEx/plugins"
$ReleasePackage = "$ReleaseStaging/BepInEx.SkorskiMod.zip"

Write-Host "Build artifacts: $ArtifactsSpec"

Write-Host "Copy outputs to client directory: $ClientDir"
Copy-Item -Path $ArtifactsSpec -Destination $ClientDir -Force

Write-Host "Copy outputs to server directory: $ServerDir"
Copy-Item -Path $ArtifactsSpec -Destination $ServerDir -Force

Write-Host "Copy outputs to release directory: $ArtifactDirForZip"
Copy-Item -Path $ArtifactsSpec -Destination $ArtifactDirForZip -Force
Copy-Item -Path $ArtifactsSpec -Destination $ReleaseStaging -Force


Write-Host "Zipping release package:  $ZipStaging -> $ReleasePackage"
Compress-Archive -Path $ZipStaging/* -DestinationPath $ReleasePackage -Force

Write-Host "Deploy script complete"