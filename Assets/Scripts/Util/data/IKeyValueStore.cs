public interface IKeyValueStore
{
	bool HasKey(string key);
	string GetString(string key, string defaultValue = "");
	void SetString(string key, string value);
	void DeleteKey(string key);
	void Save();
}
