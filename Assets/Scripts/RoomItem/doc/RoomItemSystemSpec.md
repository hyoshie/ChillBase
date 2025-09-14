# RoomItem システム設計仕様書（最新版）

## 概要

RoomItem システムは、ポモドーロアプリ内で使用する **部屋アイテムとキャラクター着せ替え** の管理機構である。  
ユーザーはショップでアイテムを購入し、キャラクターや部屋に装備させることができる。

本システムは以下の 3 層構造で設計されている。

- **データ層 (Repository)**: 永続化やロード/セーブを担当
- **アプリケーション層 (ServiceSO)**: ビジネスロジックやユースケースを提供
- **状態層 (StateSO)**: 所持状況や装備状態を保持し、イベントを発火

さらに、シーンごとの **デフォルト所持・装備** の仕組みを導入している。

---

## クラス構成

### 1. RoomItemRepository

- **責務**
  - 所持アイテム一覧と **シーン別装備マップ** の保存/読み込み
  - PlayerPrefs を利用した永続化
- **主なメソッド**
  - `LoadOwned()`: 所持アイテム一覧をロード
  - `SaveOwned(HashSet<string>)`: 所持アイテム一覧を保存
  - `LoadEquippedByScene()`: シーン別装備マップをロード
  - `SaveEquippedByScene(EqBySceneDto)`: シーン別装備マップを保存
  - `GetEquippedMap(dto, sceneId)`: 指定シーンの装備マップを取得
  - `SetEquippedMap(dto, sceneId, map)`: 指定シーンの装備マップを更新

### 2. RoomItemStateSO

- **責務**
  - 現在シーンにおける所持アイテムと装備状態を保持
  - 変化があった際にイベントを発火
- **主要プロパティ**
  - `Owned`: 所持アイテム ID の集合 (`IReadOnlyCollection<string>`)
  - `EquippedMap`: 装備マップ (slotId → itemId)
  - `DefaultSlot`: デフォルトスロット ID (例: avatar/body)
- **イベント**
  - `OnOwnedChanged(IReadOnlyCollection<string>)`
  - `OnEquippedChangedBySlot(string slotId, string itemId)`
  - `OnReset()`

### 3. RoomItemServiceSO

- **責務**
  - アイテムの購入、装備、解除などアプリケーションロジックを提供
  - Repository と State を橋渡しする
  - **RoomStateSO** と連携して、シーンごとに装備を切替
  - **必須装備ルール**: _Clothes_ カテゴリのアイテムは未装備状態にできない（解除不可）。装備の切替は可（別の _Clothes_ を装備）。
- **主要メソッド**
  - `ReloadAllFromStorage()`: 永続データをロードして State に反映。初回ならデフォルトを適用
  - `SaveAll()`: 所持・装備状態を永続化
  - `ResetAll()`: 全データをリセットし、現在シーンのデフォルトを再適用
  - `IsOwned(string itemId)`: 所持判定
  - `TryAdd(string itemId)`: 購入処理（所持に追加）
  - `GetEquipped(string slotId)`: 指定スロットの装備アイテムを取得
  - `Equip(RoomItemDef item)`: 複数スロットへ装備
  - `Unequip(RoomItemDef item)`: 複数スロットを解除
  - `IsEquipped(RoomItemDef item)`: 装備判定

### 4. RoomItemDef (SO)

- **責務**
  - アイテムの静的データを保持
- **主なフィールド**
  - `id`, `displayName`, `price`, `shopIcon`
  - `category: RoomItemCategoryDef`
  - `visuals: RoomItemVisual[]` (スロットごとの見た目)
  - `allowedRooms: List<RoomDef>`
- **主なメソッド**
  - `GetSpriteFor(string slotId)`: スロットに対応するスプライト取得
  - `GetAllSlots()`: 使用スロット一覧を返す
  - `CanUseIn(string roomId)`: 使用可能判定

### 5. RoomItemCategoryDef (SO)

- **責務**
  - アイテムカテゴリを定義
- **主なフィールド**
  - `id`, `displayName`, `icon`, `sortOrder`

### 6. RoomDef (SO)

- **責務**
  - 部屋の静的データを保持
- **主なフィールド**
  - `id`, `displayName`, `price`, `icon`
  - `defaultOwned: List<RoomItemDef>`
  - `defaultEquipped: List<RoomItemDef>`
- **補足**
  - デフォルト装備は必ず `defaultOwned` に含まれるように整合性チェック済み
  - `BuildDefaultEquippedMap()` により slotId → itemId のマップを構築

### 7. RoomStateSO (SO)

- **責務**
  - 現在のシーン ID を保持
  - `OnChanged` イベントで切替を通知

---

## データフロー

1. **起動時**

   - `RoomItemServiceSO.ReloadAllFromStorage()` 実行
   - 所持リスト & シーン別装備マップをロード
   - 未保存のシーンなら `RoomDef` のデフォルト所持・装備を適用

2. **シーン切替**

   - `RoomStateSO.Set(id)` → `RoomItemServiceSO.HandleRoomChanged`
   - 新しいシーン ID に応じた装備マップをロード
   - 未保存ならデフォルト適用

3. **アイテム購入**

   - `TryAdd(itemId)` 実行
   - State を更新 → Repository 保存 → イベント発火

4. **装備/解除**

   - `Equip/Unequip(RoomItemDef)` 実行
   - State を更新 → Repository 保存 → イベント発火
   - **Clothes カテゴリは解除不可。** 装備中に別の _Clothes_ を選ぶことでのみ切替可能。

5. **リセット**
   - `RoomItemServiceSO.ResetAll()`
   - 全 PlayerPrefs を削除
   - 現在シーンの `defaultOwned` と `defaultEquipped` を再適用

---

## 設計の特徴

- **3 層分離**: Repository / Service / State
- **イベント駆動**: State の変化を購読することで UI を自動更新
- **シーン別管理**: 装備はシーンごとに分離して保存
- **デフォルト適用**: どのシーンでも必ず最低限の所持・装備状態が保証される

---

## 今後の拡張

- CI（GitHub Actions）で UnityTest を実行し、デフォルトカテゴリが揃っているか確認
- カテゴリごとのソート順やタブ UI の柔軟化
- デフォルト装備の必須カテゴリ（例: 必ず服カテゴリが 1 つ装備されている）をテストで担保
- **必須カテゴリの担保**: _Clothes_ は常に 1 つ以上装備されるよう Service が強制（解除操作は無効化／装備の置換のみ許可）。
