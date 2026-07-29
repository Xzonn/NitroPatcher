#!/usr/bin/env bash
set -euo pipefail

version="${XZ_VERSION:-}"
version="${version#v}"
display_version="${version%%-*}"
if [[ ! "$display_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  display_version="1.6.0"
fi
build_number="${XZ_BUILD_NUMBER:-1}"

app_output="artifacts/maui-macos-arm64"
cli_output="artifacts/cli-osx-arm64"

signing_args=(-p:EnableCodeSigning=false)
if [[ -n "${XZ_CODESIGN_KEY:-}" ]]; then
  : "${XZ_CODESIGN_PROVISION:?XZ_CODESIGN_PROVISION is required for signed builds}"
  signing_args=(
    -p:EnableCodeSigning=true
    "-p:CodesignKey=${XZ_CODESIGN_KEY}"
    "-p:CodesignProvision=${XZ_CODESIGN_PROVISION}"
    -p:UseHardenedRuntime=true
  )
fi

dotnet publish NitroPatcherMaui/NitroPatcherMaui.csproj \
  -c Release \
  -f net8.0-maccatalyst \
  -r maccatalyst-arm64 \
  --output "$app_output" \
  -p:NitroPatcherTargetFrameworks=net8.0-maccatalyst \
  "-p:ApplicationDisplayVersion=${display_version}" \
  "-p:ApplicationVersion=${build_number}" \
  "${signing_args[@]}"

dotnet publish NitroPatcherCli/NitroPatcherCli.csproj \
  -c Release \
  -f net8.0 \
  -r osx-arm64 \
  --self-contained true \
  --output "$cli_output" \
  "-p:Version=${display_version}" \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false

app_path=""
built_app_path="NitroPatcherMaui/bin/Release/net8.0-maccatalyst/maccatalyst-arm64/NitroPatcherMaui.app"
if [[ -d "$built_app_path" ]]; then
  app_path="$app_output/NitroPatcherMaui.app"
  ditto "$built_app_path" "$app_path"
else
  app_path="$(find "$app_output" -maxdepth 1 -type d -name '*.app' -print -quit)"
fi
if [[ -z "$app_path" ]]; then
  echo "Mac Catalyst publish did not produce an app bundle." >&2
  exit 1
fi

app_executable="$app_path/Contents/MacOS/NitroPatcherMaui"
file "$app_executable" | grep -q 'arm64'
file "$cli_output/NitroPatcherCli" | grep -q 'arm64'
