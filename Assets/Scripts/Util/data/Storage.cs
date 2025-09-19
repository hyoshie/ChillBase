// グローバルな切替ポイント（全Repository共通で使う）
public static class Storage
{
	static IKeyValueStore _store = new PlayerPrefsStore();
	static string _prefix = ""; // テスト分離用の接頭辞

	public static void Use(IKeyValueStore store) => _store = store ?? new PlayerPrefsStore();
	public static void SetKeyPrefix(string prefix) => _prefix = prefix ?? "";

	// 便利ラッパ
	public static bool Has(string key) => _store.HasKey(_prefix + key);
	public static string GetString(string key, string def = "") => _store.GetString(_prefix + key, def);
	public static void SetString(string key, string value) { _store.SetString(_prefix + key, value); _store.Save(); }
	public static void DeleteKey(string key) { _store.DeleteKey(_prefix + key); _store.Save(); }

#if UNITY_INCLUDE_TESTS
	// テスト用スコープ（usingで元に戻す）
	public static System.IDisposable PushInMemoryScope(string prefix = "TEST_")
	{
		var prevStore = _store;
		var prevPrefix = _prefix;
		Use(new InMemoryStore());
		SetKeyPrefix(prefix);
		return new Scope(() => { Use(prevStore); SetKeyPrefix(prevPrefix); });
	}
	private sealed class Scope : System.IDisposable
	{
		private readonly System.Action _onDispose;
		public Scope(System.Action onDispose) { _onDispose = onDispose; }
		public void Dispose() => _onDispose?.Invoke();
	}
#endif
}
