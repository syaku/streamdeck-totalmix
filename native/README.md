# native/librtmidi.dylib

RtMidi.Core (nuget) 同梱の librtmidi.dylib は x86_64 のみで、Apple Silicon では
DllNotFoundException になる (micdah/RtMidi.Core#37)。この arm64 ビルドを publish 時に
上書き同梱する (design レコード 6)。

- 由来: Homebrew formula `rtmidi` 6.0.0_1 (arm64 bottle) の librtmidi.7.dylib
- 取得手順: `brew install rtmidi` → `cp -L /opt/homebrew/lib/librtmidi.dylib native/`
  → `install_name_tool -id @rpath/librtmidi.dylib` → `codesign -f -s -`
- 依存: システムフレームワークのみ (CoreMIDI / CoreAudio / CoreServices / CoreFoundation)
- アーキテクチャ: arm64 のみ。Intel Mac 向けには x86_64 版の同梱が別途必要 (未対応・design Risks)
