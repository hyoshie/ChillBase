// 本番：PlayerPrefs バックエンド
public sealed class PlayerPrefsStore : IKeyValueStore
{
	public bool HasKey(string key) => UnityEngine.PlayerPrefs.HasKey(key);
	public string GetString(string key, string defaultValue = "") =>
			UnityEngine.PlayerPrefs.GetString(key, defaultValue);
	public void SetString(string key, string value) =>
			UnityEngine.PlayerPrefs.SetString(key, value);
	public void DeleteKey(string key) => UnityEngine.PlayerPrefs.DeleteKey(key);
	public void Save() => UnityEngine.PlayerPrefs.Save();
}
