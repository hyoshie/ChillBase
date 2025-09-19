# Storage 抽象化仕様書

## 概要

本システムでは、従来 **`PlayerPrefs`** を直接利用していたデータ保存処理を、  
**`Storage` クラス**を介して行うように抽象化した。

これにより、

- 実行環境（実機/エディタ/テスト）に応じたストレージ実装の切替
- PlayerPrefs に依存しない **InMemoryStore** を使ったテスト実行  
  が可能になった。

---

## 設計方針

- **Repository 層は Storage のみを利用**し、`PlayerPrefs` API を直接呼ばない。
- Storage は「Key-Value ストア」として最小限の操作のみ提供する。
- テスト実行時には `InMemoryStore` に切り替えて PlayerPrefs を汚さずに検証可能。
- キーには `prefix` を付与して名前空間を分け、テストや環境ごとに衝突を回避する。

---

## クラス構成

### IKeyValueStore インターフェイス

```csharp
public interface IKeyValueStore
{
    bool HasKey(string key);
    string GetString(string key, string defaultValue = "");
    void SetString(string key, string value);
    void DeleteKey(string key);
    void Save();
}
```

### PlayerPrefsStore

- 実機/エディタでの通常保存用
- 内部的には `PlayerPrefs` を呼び出す実装
- 永続化 (`Save()`) も実行される

### InMemoryStore

- テスト用の「保存風」実装
- データは Dictionary に保持され、プロセス終了で消える
- I/O を発生させないため、**高速かつ PlayerPrefs を汚さない**

### Storage クラス（グローバル切替ポイント）

```csharp
public static class Storage
{
    static IKeyValueStore _store = new PlayerPrefsStore();
    static string _prefix = "";

    public static void Use(IKeyValueStore store);
    public static void SetKeyPrefix(string prefix);

    public static bool Has(string key);
    public static string Get(string key, string def = "");
    public static void Set(string key, string value);
    public static void Delete(string key);
}
```

---

## テスト用ユーティリティ

`UNITY_INCLUDE_TESTS` シンボルが有効な場合、  
テスト用にスコープを切り替えるヘルパーが利用可能。

```csharp
using (Storage.PushInMemoryScope("TEST_"))
{
    // この間の Repository.Save() / Load() は InMemoryStore に書き込まれる
    // PlayerPrefs は一切触らない
}
```

- `prefix = "TEST_"` により、本番キーとの衝突を防ぐ
- `Dispose()` 時に元の Store/Prefix に自動復帰

---

## 利用例

### 本番

```csharp
RoomItemRepository.SaveOwned(new [] { "shirt001" });
```

→ PlayerPrefs に保存される。

### テスト

```csharp
[Test]
public void SaveAndLoad_Roundtrip_WorksInMemory()
{
    using (Storage.PushInMemoryScope())
    {
        RoomItemRepository.SaveOwned(new [] { "shirt001" });
        var loaded = RoomItemRepository.LoadOwned();
        Assert.Contains("shirt001", loaded.items);
    }
}
```

→ InMemoryStore に保存 → ロードでも同じ結果を取得 → PlayerPrefs は未変更。

---

## メリット

- ✅ PlayerPrefs を汚さずにテスト可能
- ✅ 実機と同じコードパスを通す「統合的ユニットテスト」
- ✅ 名前空間（prefix）でテストと本番を分離可能
- ✅ Repository 側は変更不要（Storage API 経由で一貫利用）
