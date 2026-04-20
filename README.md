# VMCFaceShortcutTransitionMod

VirtualMotionCapture (VMC) の表情ショートカット操作に、補間付きの遷移時間を追加する Mod です。
`ControlWPFWindow.DoKeyAction` を Harmony でパッチして動作します。

## 動作仕様
- 対象はショートカット内の表情のみです。
- 外部 OSC からの BlendShape 入力経路はパッチしません。
- 遷移中に次の表情ショートカットが来た場合、現在の遷移をキャンセルして新しい遷移を開始します。
- 設定は JSON から読み込みます。起動時とVMCの詳細設定のロード済みModの `[Setting]` ボタン呼び出し時に再読込されます。

## インストール
1. `VMCFaceShortcutTransitionMod.dll` を VMC の `Mods` フォルダに配置します。
2. Mod 読込可能な VMC で起動します。

## JSON 設定ファイル
初回起動時に、Mod DLL と同じフォルダに以下が生成されます。

- `VMCFaceShortcutTransitionMod.json`

### ルート設定項目

| キー | 型 | 既定値 | 説明 |
| --- | --- | --- | --- |
| `Enable` | bool | `true` | Mod 全体の有効/無効。`false` の場合はパッチ処理を行わず、VMC 本来の挙動になります。 |
| `DefaultTransitionSec` | float | `0.1` | アクション個別設定がない場合の遷移秒数。`0` 以下は即時反映（補間なし）として扱われます。 |
| `TickMs` | int | `16` | 補間値を適用する間隔（ミリ秒）。値が小さいほど滑らかですが負荷は増えます。`1` 未満は `1` に補正されます。 |
| `ApplyOnlyWhenSoftChangeFalse` | bool | `true` | `true` の場合、`KeyAction.SoftChange == true` のアクションには遷移を適用しません。 |
| `ActionRules` | array | `[]` | アクション名ごとの個別設定。`KeyAction.Name` に対して大文字小文字を無視して照合します。 |

### ActionRules 項目

| キー | 型 | 既定値 | 説明 |
| --- | --- | --- | --- |
| `ActionName` | string | `""` | ルール対象のアクション名（`KeyAction.Name`）。 |
| `Enable` | bool | `true` | そのアクションの遷移ルール有効/無効。`false` の場合は遷移せず VMC 本来の即時反映に戻ります。 |
| `TransitionSec` | float | `0.2` | そのアクション専用の遷移秒数。`0` 以下は即時反映（補間なし）です。 |

### 設定例

```json
{
  "Enable": true,
  "DefaultTransitionSec": 0.1,
  "TickMs": 16,
  "ApplyOnlyWhenSoftChangeFalse": true,
  "ActionRules": [
    {
      "ActionName": "Smile",
      "Enable": true,
      "TransitionSec": 0.15
    },
    {
      "ActionName": "Angry",
      "Enable": false,
      "TransitionSec": 0.2
    }
  ]
}
```

## ビルド
1. `VMCFaceShortcutTransitionMod/VMCFaceShortcutTransitionMod.csproj` を開きます。
2. .NET Framework 4.8 クラスライブラリとしてビルドします。
3. NuGet パッケージ（HarmonyX 2.16.1 / ILRepack）を復元します。
4. ビルド後、ILRepack で必要な依存 DLL を `VMCFaceShortcutTransitionMod.dll` に統合します。

既定の VMC 参照先:
- `C:\TOOL\VirtualMotionCapture`

MSBuild プロパティ `VMCDirectory` で上書きできます。
