using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoomRepository
{
	static readonly string KeyOwnedRooms = RoomConstants.KeyOwnedRooms;
	static readonly string KeyCurrentRoom = RoomConstants.KeyCurrentRoom;

	[Serializable] class SaveDto { public int version = 1; public List<string> items = new(); }

	// ---- 読み取り（IOのみ） ----
	public static HashSet<string> LoadOwned()
	{
		var raw = PlayerPrefs.GetString(KeyOwnedRooms, "");
		if (string.IsNullOrEmpty(raw)) return new HashSet<string>();
		try
		{
			var list = JsonUtility.FromJson<SaveDto>(raw)?.items ?? new List<string>();
			return new HashSet<string>(list);
		}
		catch { return new HashSet<string>(); }
	}

	public static string LoadCurrent()
	{
		return PlayerPrefs.GetString(KeyCurrentRoom, "") ?? "";
	}

	// ---- 書き込み（IOのみ） ----
	public static void SaveOwned(IEnumerable<string> items)
	{
		var dto = new SaveDto { items = items?.ToList() ?? new List<string>() };
		PlayerPrefs.SetString(KeyOwnedRooms, JsonUtility.ToJson(dto));
		PlayerPrefs.Save();
	}

	public static void SaveCurrent(string roomId)
	{
		PlayerPrefs.SetString(KeyCurrentRoom, roomId ?? "");
		PlayerPrefs.Save();
	}
}
