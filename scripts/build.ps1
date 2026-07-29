python scripts\update_versions.py
$version = ${env:XZ_VERSION} -Replace '^v', ''
$version = ($version -Split '-', 2)[0]
if ($version -notmatch '^\d+\.\d+\.\d+$') {
  $version = '1.6.0'
}
nuget restore
Push-Location -Path "NitroPatcher"
msbuild /p:Configuration=Release /p:TargetFramework=net47 /p:OutDir="../artifacts/gui" /verbosity:minimal
Pop-Location
dotnet publish -c Release NitroPatcherCli -f net6.0 --os win --output "artifacts/cli-win" -p:Version=$version
dotnet publish -c Release NitroPatcherCli -f net6.0 --os linux --output "artifacts/cli-linux" -p:Version=$version
dotnet publish -c Release NitroPatcherCli -f net6.0 --os osx --output "artifacts/cli-osx" -p:Version=$version
