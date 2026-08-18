#!/usr/bin/env bash
# .sdPlugin の配布レイアウトを組み立てる。
# 出力: bin/publish/{manifest.json,Images,PropertyInspector,previews,win/,mac/}
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/streamdeck-totalmix.csproj"
output="$root/bin/publish"
target="${1:-all}"

publish_runtime() {
	local rid="$1" folder="$2"
	rm -rf "${output:?}/$folder"
	dotnet publish "$project" -c Release -r "$rid" --self-contained true -o "$output/$folder"
}

case "$target" in
win) publish_runtime win-x64 win ;;
mac) publish_runtime osx-arm64 mac ;;
all)
	publish_runtime win-x64 win
	publish_runtime osx-arm64 mac
	;;
*)
	echo "usage: ${BASH_SOURCE[0]} [win|mac|all]" >&2
	exit 1
	;;
esac

if [ -d "$output/mac" ]; then
	# RtMidi.Core が同梱する librtmidi.dylib は x86_64 のみで Apple Silicon ではロードに失敗する。
	# 同梱は .targets の無条件コピーで RID を見ないため、publish 後にここで arm64 版を被せる。
	cp "$root/native/librtmidi.dylib" "$output/mac/librtmidi.dylib"
fi

for asset in manifest.json Images PropertyInspector previews; do
	rm -rf "${output:?}/$asset"
	cp -R "$root/$asset" "$output/$asset"
done

echo "published to $output"
