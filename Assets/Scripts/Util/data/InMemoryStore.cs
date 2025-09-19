// テスト/エディタ用：InMemory バックエンド
public sealed class InMemoryStore : IKeyValueStore
{
	private readonly System.Collections.Generic.Dictionary<string, string> _map = new();
	public bool HasKey(string key) => _map.ContainsKey(key);
	public string GetString(string key, string defaultValue = "") =>
			_map.TryGetValue(key, out var v) ? v : defaultValue;
	public void SetString(string key, string value) => _map[key] = value ?? "";
	public void DeleteKey(string key) => _map.Remove(key);
	public void Save() { /* no-op */ }
}
