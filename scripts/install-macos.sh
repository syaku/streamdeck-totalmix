#!/usr/bin/env bash
# publish.sh の出力をこの Mac の Stream Deck プラグインフォルダへ同期する。
# rsync -a を使う(--delete は付けない)。cp 系だと稼働中のプラグインプロセスが
# 載っている実行ファイルを上書きして SIGKILL され、Stream Deck がコピー途中の
# ツリーを掴んで再起動するため。--delete を付けないのは、同居する
# mac/pluginlog.log(実機確認の証跡)を消さないため。
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="$root/bin/publish"
plugin_dir="$HOME/Library/Application Support/com.elgato.StreamDeck/Plugins/de.shells.totalmix.sdPlugin"

bash "$root/scripts/publish.sh" all

if [ ! -d "$output" ]; then
	echo "publish output not found: $output" >&2
	exit 1
fi

mkdir -p "$plugin_dir"
rsync -a "$output/" "$plugin_dir/"

echo "installed to $plugin_dir"
echo
echo "Stream Deck アプリを再起動すると新しいプラグインが読み込まれます:"
echo "  osascript -e 'quit app \"Stream Deck\"' && open -a \"Stream Deck\""
