Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Installs additional shared frameworks for testing purposes
function InstallDotNetSharedFramework([string]$dotnetRoot, [string]$version) {
  $fxDir = Join-Path $dotnetRoot "shared\Microsoft.NETCore.App\$version"

  if (!(Test-Path $fxDir)) {
    $installScript = GetDotNetInstallScript $dotnetRoot
    & $installScript -Version $version -InstallDir $dotnetRoot -Runtime dotnet

    if($lastExitCode -ne 0) {
      throw "Failed to install shared Framework $version to '$dotnetRoot' (exit code '$lastExitCode')."
    }
  }
}

# The following frameworks and tools are used only for testing.
# Do not attempt to install them in source build.
if ($env:DotNetBuildFromSource -ne "true") {
  InstallDotNetSharedFramework $env:DOTNET_INSTALL_DIR "1.1.2"
}
