#!/usr/bin/env bash
set -euo pipefail

version="${XZ_VERSION:-}"
version="${version#v}"
display_version="${version%%-*}"
if [[ ! "$display_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  display_version="1.6.0"
fi
build_number="${XZ_BUILD_NUMBER:-1}"
output="artifacts/maui-android"

dotnet publish NitroPatcherMaui/NitroPatcherMaui.csproj \
  -c Release \
  -f net8.0-android \
  --output "$output" \
  "-p:ApplicationDisplayVersion=${display_version}" \
  "-p:ApplicationVersion=${build_number}" \
  -p:AndroidPackageFormats=apk

apk_path="$(find "$output" -type f -name '*-Signed.apk' -print -quit)"
if [[ -z "$apk_path" ]]; then
  apk_path="$(find "$output" -type f -name '*.apk' -print -quit)"
fi
if [[ -z "$apk_path" ]]; then
  echo "Android publish did not produce an APK." >&2
  exit 1
fi

artifact_version="${version:-dev}"
zip -j "artifacts/NitroPatcherMAUI.${artifact_version}-android.zip" "$apk_path"
