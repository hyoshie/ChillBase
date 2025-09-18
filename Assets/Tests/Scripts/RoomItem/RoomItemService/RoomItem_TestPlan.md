# RoomItem システム テスト計画

本ドキュメントは、RoomItem
システムのドメインコアを中心としたテストケースを整理したものである。\
優先度を P0（最優先）→ P1（重要）→ P2（余裕あれば
UI）と段階的に設定している。

------------------------------------------------------------------------

## P0（最優先：ドメインのコア）

### 1. 服は未装備にできない（解除禁止）

-   **条件**
    -   装備中の Clothes を Unequip → `false` / 状態不変
    -   Clothes → 別の Clothes に Equip は成功（置き換え）
-   **テスト名**
    -   `Service_Clothes_CannotUnequip_ReturnsFalse_StateUnchanged()`
    -   `Service_Clothes_EquipOther_ReplacesSuccessfully()`

### 2. シーン別装備マップの切替

-   **条件**
    -   sceneA で itemA 装備
    -   sceneB に切替 → 装備は空（or 他）
    -   sceneA に戻ると itemA が装備されたまま
-   **テスト名**
    -   `Service_Equip_SceneIsolation_AandBIndependent()`

### 3. デフォルト適用（初回のみ）

-   **条件**
    -   保存が空の sceneX で `ReloadAllFromStorage()`
    -   `defaultOwned` が Owned に含まれる
    -   `defaultEquipped` が EquippedMap に反映
    -   2 回目以降（保存あり）は上書きされない
-   **テスト名**
    -   `Service_FirstEnter_AppliesDefaults_ThenNotOverride()`

### 4. Owned の補強ルール

-   **条件**
    -   `defaultEquipped` にある ID が Owned に無い場合、自動で Owned
        に追加
-   **テスト名**
    -   `Service_DefaultEquipped_AutoAugmentsOwned()`

### 5. 保存タイミング

-   **条件**
    -   TryAdd / Equip / Unequip 実行直後に Repository セーブが呼ばれる
    -   モック or インメモリで検証
-   **テスト名**
    -   `Service_SaveOn_TryAdd_Equip_Unequip_CallsRepository()`

### 6. イベント発火の正しさ

-   **条件**
    -   `SetEquipped(slot, id)` で当該スロットのみ
        `OnEquippedChangedBySlot`
    -   同値更新はイベント発火なし
    -   `SetOwned` も差分時のみ `OnOwnedChanged`
-   **テスト名**
    -   `State_SetEquipped_RaisesEventOnce_PerSlot_NoEventOnSameValue()`

------------------------------------------------------------------------

## P1（重要：周辺仕様/退行防止）

### 7. ResolveSlots と DefaultSlot のフォールバック

-   **条件**
    -   visuals 無し → DefaultSlot で装備できる
    -   visuals に同一スロット重複があっても一度だけ適用
-   **テスト名**
    -   `Def_ResolveSlots_FallsBackToDefaultSlot_WhenNoVisuals()`

### 8. CanUseIn(roomId) の挙動

-   **条件**
    -   allowedRooms 空 → 全許可
    -   合致する roomId → true / しない → false
-   **テスト名**
    -   `ItemDef_CanUseIn_AllowsWhenEmpty_DisallowsWhenMismatch()`

### 9. Repository ラウンドトリップ

-   **条件**
    -   Owned / byScene Equipped を保存 → ロードで一致
    -   DTO が空/未初期化でも例外なくデフォルト復元
-   **テスト名**
    -   `Repository_Roundtrip_Owned_And_EquippedByScene_Persists()`

### 10. ResetAll のデフォルト復帰

-   **条件**
    -   消去後、現在シーンの `defaultOwned` / `defaultEquipped`
        が反映・保存される
-   **テスト名**
    -   `Service_ResetAll_ReappliesCurrentSceneDefaults()`

### 11. SyncScene の共通化挙動

-   **条件**
    -   forceDefaults=false：保存優先
    -   forceDefaults=true：保存があっても初回扱いでデフォルト適用
-   **テスト名**
    -   `Service_SyncScene_ForceDefaults_OverridesSavedForFirstTime()`

------------------------------------------------------------------------

## P2（余裕があれば：UI/統合スモーク）

### 12. UI スモーク（PlayMode）

-   **条件**
    -   タブ切替で件数が変わる
    -   購入 → 装備でボタン表示が Buy→Using に変わる
    -   装備切替時、他行もイベントで表示更新される
-   **テスト名**
    -   `UISmoke_TabSwitch_ChangesCount()`
    -   `UISmoke_BuyToEquip_ButtonStateChanges()`
    -   `UISmoke_EquipSwitch_RefreshesOtherRows()`

### 13. カテゴリ UI

-   **条件**
    -   clothes の行は装備中に「Equipped (Required)」非活性表示
    -   解除ボタン（またはトグル）は出ない/押せない
-   **テスト名**
    -   `UI_Category_Clothes_DisablesUnequipButton()`

------------------------------------------------------------------------

## 優先度まとめ

-   **P0**: ドメインの正しさを保証するため最優先（まずここを通す）
-   **P1**: 周辺仕様・退行防止（徐々にカバー）
-   **P2**: UI スモーク・見た目の挙動（余裕があれば）

------------------------------------------------------------------------

## テスト戦略の補足

-   デフォルトは **Storage を InMemory
    に差し替えて検証**する方式を採用する。
    -   本番と同じ保存フロー（Save → Load）を通しつつ、I/O
        は発生しないため高速・安定。\
    -   保存直後に状態が正しく反映されることまで確認できる。\
-   一方で「呼ばれた回数」など厳密な検証が必要な場合のみ **Repository
    をモック化**する。\
-   実務的には **まず InMemory で全 P0/P1
    をカバーし、特殊ケースのみモック**が最も効率的。
