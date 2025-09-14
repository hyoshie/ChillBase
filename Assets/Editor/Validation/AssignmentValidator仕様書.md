# AssignmentValidator 仕様書

## 概要

Unity Editor 上で、**ScriptableObject / Prefab / シーン上の
MonoBehaviour** に対し、\
**インスペクタで割り当てが必須なフィールド**が未設定（null）の場合に検出・警告を出すエディタ拡張。

Play
モードに入る直前やプロジェクト変更時に自動実行され、未割り当てがある場合は
**Play を止めるかどうか選択**できる。

また、フィールドに `[AllowNull]` 属性を付けることで、**null
許容（検証対象外）**にすることが可能。

------------------------------------------------------------------------

## 検査対象

### アセット（自動検索されるフォルダ）

-   `Assets/GameConfig`
-   `Assets/Data`
-   `Assets/Prefabs`

### 対象となる型

-   **ScriptableObject**
-   **Prefab 内の MonoBehaviour**
-   **シーン上の MonoBehaviour**

### 型のフィルタ

-   クラスが属するアセンブリ名が、以下いずれかの**接頭辞で始まるもの**に限定
    -   `Assembly-CSharp`
    -   `ScriptsAssembly`\
        → 例: `ScriptsAssembly`, `ScriptsAssembly.Editor`,
        `ScriptsAssembly.Runtime` なども含む

※これにより、外部アセットや Unity 標準コンポーネント（TextMeshPro
など）は除外される。

------------------------------------------------------------------------

## フィールド検証ルール

以下を満たすフィールドが検証対象： - 型が `UnityEngine.Object`
を継承している（例: `GameObject`, `Sprite`, `Material`, `AudioClip`,
他） - `public` フィールド、または `private` でも `[SerializeField]`
が付いている - `[AllowNull]` 属性が付いていない

上記条件を満たすフィールドが **null の場合に警告**される。

------------------------------------------------------------------------

## 実行タイミング

1.  **プロジェクトのアセット変更時**
    -   デバウンス付き（0.3秒後にまとめて実行）\
    -   ScriptableObject / Prefab のみ軽く検証\
    -   警告を Console に表示
2.  **Play モードへ入る直前**
    -   ScriptableObject / Prefab / シーン上 MonoBehaviour をすべて検証\
    -   未割り当てがあればダイアログを表示
        -   **「中止する」** → Play を止める & Console に警告展開\
        -   **「続行」** → Play 継続（警告は残る）
3.  **手動実行**
    -   メニュー `Tools/Validation/Validate All(Scene,SO,Prefab)`\
    -   すべて検証し、結果を Console に出力

------------------------------------------------------------------------

## 付属機能

-   （コメントアウト中）`PrintDetectedAssemblies`
    -   MenuItem を有効化すると、対象フォルダ・シーン・Prefab
        から発見されたクラスのアセンブリ名一覧を Console に出力できる。\
    -   フィルタ設定（`AllowedAssemblyPrefixes`）を調整する際の診断用。

------------------------------------------------------------------------

## 出力例

-   警告ログ形式：

        [RoomConfig] RoomItemServiceSO.roomState が未割り当てです。

    -   `[プレフィックス] クラス名.フィールド名 が未割り当てです。`
    -   プレフィックスは SO 名 / Prefab 名 / シーン上 GameObject
        名など。

------------------------------------------------------------------------

👉 この仕様のまま Prefab で拾えない場合は、\
- Prefab が `Assets/Prefabs` 以外に置いてある\
- Prefab 内コンポーネントがホワイトリスト外アセンブリに属している\
- フィールド条件に合っていない（SerializeField なし、型が非
UnityEngine.Object）\
のいずれかが原因になるはずです。
