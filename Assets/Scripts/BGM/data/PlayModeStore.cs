using UnityEngine;


[System.Serializable]
class PlayModeDto
{
	public int version = 1;          // ★ 将来の拡張用
	public string playMode = nameof(PlayMode.Sequential);
}

static class PlayModeStore
{
	private const string Key = "music_play_mode_v1";

	public static void Save(PlayMode mode)
	{
		var dto = new PlayModeDto
		{
			version = 1,
			playMode = mode.ToString()
		};
		string json = JsonUtility.ToJson(dto);
		PlayerPrefs.SetString(Key, json);
		PlayerPrefs.Save();
	}

	public static PlayMode Load()
	{
		string raw = PlayerPrefs.GetString(Key, "");
		if (string.IsNullOrEmpty(raw))
			return PlayMode.Sequential; // デフォルト

		try
		{
			var dto = JsonUtility.FromJson<PlayModeDto>(raw);
			if (dto == null) return PlayMode.Sequential;

			// 将来 version で分岐できるようにする
			switch (dto.version)
			{
				case 1:
				default:
					if (System.Enum.TryParse(dto.playMode, out PlayMode parsed))
						return parsed;
					return PlayMode.Sequential;
			}
		}
		catch
		{
			return PlayMode.Sequential;
		}
	}
}
