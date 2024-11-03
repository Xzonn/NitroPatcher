python scripts\update_versions.py
nuget restore
Push-Location -Path "NitroPatcher"
msbuild /p:Configuration=Release /p:TargetFramework=net47 /p:OutDir="../artifacts/gui" /verbosity:minimal
Pop-Location
dotnet publish -c Release NitroPatcherCli -f net6.0 --os win --output "artifacts/cli-win"
dotnet publish -c Release NitroPatcherCli -f net6.0 --os linux --output "artifacts/cli-linux"
dotnet publish -c Release NitroPatcherCli -f net6.0 --os osx --output "artifacts/cli-osx"
