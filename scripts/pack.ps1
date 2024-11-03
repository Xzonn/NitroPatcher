$version = $(${env:XZ_VERSION} -Replace '^v', '')
Compress-Archive -Path "artifacts/gui/NitroPatcher.exe" -DestinationPath "artifacts/NitroPatcher.$version.zip" -Force
Compress-Archive -Path "artifacts/cli-win/NitroPatcherCli.exe" -DestinationPath "artifacts/NitroPatcherCli.$version-win-x64.zip" -Force
Compress-Archive -Path "artifacts/cli-linux/NitroPatcherCli" -DestinationPath "artifacts/NitroPatcherCli.$version-linux-x64.zip" -Force
Compress-Archive -Path "artifacts/cli-osx/NitroPatcherCli" -DestinationPath "artifacts/NitroPatcherCli.$version-osx-x64.zip" -Force
