# 装備アイテム表示システム 仕様書（最新版）

## 概要

ユーザーが所持・装備したアイテムを、部屋やキャラクターのスロットに表示するシステム。  
静止画アイテムは `Sprite`、アニメーションアイテムは `AnimatorController` を用いて UI 上に表示する。  
アイテムの所持／装備管理は **RoomItemRepository / RoomItemServiceSO / RoomItemStateSO** に分離されており、  
表示システムは `RoomItemStateSO` のイベントを購読して同期する。

---

## 構成要素

### 1. RoomItemDef (旧 ItemData)

- **役割**: 各アイテムの定義を保持する `ScriptableObject`。
- 主なフィールド:

  - `id`: アイテム識別子（文字列）
  - `displayName`: 表示名
  - `price`: 価格
  - `shopIcon`: ショップでのアイコン
  - `visuals[]`: `RoomItemVisual` 配列
    - `slot`: 適用対象スロット (`ItemSlotDef`)
    - `viewType`: 表示種別（静止画 or アニメ）
    - `sprite`: 静止画用スプライト
    - `animator`: アニメーション用コントローラ（任意）
  - `allowedRooms`: 使用可能な部屋のリスト

- 主なメソッド:
  - `GetSpriteFor(slotId)`
  - `CanUseIn(roomId)`

### 2. RoomItemRepository / RoomItemServiceSO / RoomItemStateSO

- **Repository**: PlayerPrefs を用いて所持データと装備マップを保存／読み込み
- **ServiceSO**: 購入・装備・解除などのビジネスロジックを提供
- **StateSO**: 現在の所持・装備状態を保持し、イベント発火

- 主なイベント:
  - `OnOwnedChanged(IReadOnlyCollection<string>)`
  - `OnEquippedChangedBySlot(string slotId, string itemId)`
  - `OnReset()`

### 3. RoomItemSlotController

- **役割**: 各スロットに対して現在装備されているアイテムのビューを生成／破棄する。
- 主な仕様:
  - `viewRoot` に子要素を生成
  - `SetView(RoomItemVisual visual)`  
    → 静止画 / アニメのプレハブを選択して表示
  - `Clear()` で削除

### 4. ビュープレハブ

- **StaticSpriteView**

  - 構成: `RectTransform` + `Image`
  - 表示方法: `Image.sprite` に静止画を設定
  - 設定推奨: `preserveAspect = true`, `raycastTarget = false`

- **SpriteAnimView**
  - 構成: `RectTransform` + `Image` + `Animator` + `SpriteAnimView` スクリプト  
    （Animator は **Image と同じ GameObject** に付ける）
  - 表示方法: `Animator.runtimeAnimatorController` にアイテムのコントローラを割り当て  
    → アニメーションで `Image.Source Image` を切り替え
  - 設定推奨: `preserveAspect = true`, `raycastTarget = false`

---

## Animator / AnimationClip の仕様

### Animator

- UI Image と同じ GameObject に配置
- `runtimeAnimatorController` は `RoomItemVisual.animator` を動的に設定
- **CullingMode**: `AlwaysAnimate`  
  UI (`Image`/`CanvasRenderer`) には Renderer が存在しないため  
  `CullCompletely` にすると再生されない

### AnimationClip

- 対象プロパティ: `Image (Script) > Source Image`
- バインド先: Animator が付いている同じオブジェクトの `Image` コンポーネント
- 設定:
  - **Loop Time = ON**
  - **Legacy = OFF**

---

## 表示の流れ

1. `RoomItemServiceSO.Equip(item)` → StateSO が更新  
   → `OnEquippedChangedBySlot(slotId, itemId)` が発火
2. `RoomAppearanceController` が該当スロットの `SlotController` を検索
3. `SlotController.SetView(RoomItemVisual visual)` が呼ばれる
   - `viewType=Static` → `StaticSpriteView` プレハブ生成
   - `viewType=Animated` → `SpriteAnimView` プレハブ生成
4. `StaticSpriteView` or `SpriteAnimView` が表示される
5. 装備解除時は `SlotController.Clear()` で削除

---

## 運用ルール

- **スロット**: 差し替えポイントは `Slot_[SlotId]` という空の GameObject に `SlotController` を付与
- **アニメーションアイテム**:
  - Animator は **Image と同じオブジェクト**に付ける
  - Controller/Clip のパスは自己参照にすることで名前変更に強い
- **パフォーマンス**:
  - 非表示時は `animator.enabled = false` にする
  - `CullCompletely` は使わない

---

## よくある問題と対処

- **アニメが再生されない**
  - Clip のバインド先が外れていないか確認（黄色表示は無効）
  - Clip の対象が `Image > Source Image` になっているか
  - Clip が Legacy になっていないか
- **Animator が動かない**
  - `CullCompletely` を使っていないか
  - Controller の Default State に Clip が設定されているか
- **生成位置がずれる**
  - `SlotController.viewRoot` が正しい RectTransform か確認
  - Canvas 配下にあるか確認

---

これにより、**静止画アイテム**と**アニメーションアイテム**の両方を安全に扱える構成になっている。
