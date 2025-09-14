using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// PlayerPrefs の入出力だけを担当（状態・イベント・検証は持たない）
/// 既存キーをそのまま使用:
///  - 所持: "purchased_item_ids"
///  - 装備: "equipped_map_v2"
/// </summary>
public static class RoomItemRepository
{
	const string KeyPurchased = "purchased_item_ids";
	const string KeyEquippedMap = "equipped_map_v2";

	public class OwnedDto { public int version = 1; public List<string> items = new(); }

	[Serializable]
	class EquippedDto
	{
		public int version = 1;
		public List<Entry> entries = new();
		[Serializable] public struct Entry { public string slot; public string item; }
	}

	// -------------------------
	// Owned（購入） I/O
	// -------------------------
	public static OwnedDto LoadOwned()
	{
		var raw = PlayerPrefs.GetString(KeyPurchased, "");
		if (string.IsNullOrEmpty(raw)) return new OwnedDto();

		try
		{
			return JsonUtility.FromJson<OwnedDto>(raw) ?? new OwnedDto();
		}
		catch (Exception e)
		{
			Debug.LogError($"[Repo] LoadOwned parse error: {e}");
			return new OwnedDto();
		}
	}

	public static void SaveOwned(IReadOnlyCollection<string> owned)
	{
		var dto = new OwnedDto { items = owned?.ToList() ?? new List<string>() };
		PlayerPrefs.SetString(KeyPurchased, JsonUtility.ToJson(dto));
		PlayerPrefs.Save();
	}

	// ---- 装備マップの読込/保存 ----
	public static Dictionary<string, string> LoadEquippedMap()
	{
		var raw = PlayerPrefs.GetString(KeyEquippedMap, "");
		if (string.IsNullOrEmpty(raw)) return new Dictionary<string, string>();

		try
		{
			var dto = JsonUtility.FromJson<EquippedDto>(raw) ?? new EquippedDto();
			var map = new Dictionary<string, string>();
			if (dto.entries != null)
			{
				foreach (var e in dto.entries)
				{
					var slot = e.slot ?? "";
					var item = e.item ?? "";
					if (string.IsNullOrEmpty(slot) || string.IsNullOrEmpty(item)) continue;
					map[slot] = item; // 同一スロットは最後の値を採用
				}
			}
			return map;
		}
		catch
		{
			return new Dictionary<string, string>();
		}
	}

	public static void SaveEquippedMap(IReadOnlyDictionary<string, string> map)
	{
		var dto = new EquippedDto
		{
			entries = (map ?? new Dictionary<string, string>())
										.Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
										.Select(kv => new EquippedDto.Entry { slot = kv.Key, item = kv.Value })
										.ToList()
		};
		PlayerPrefs.SetString(KeyEquippedMap, JsonUtility.ToJson(dto));
		PlayerPrefs.Save();
	}


	// ---- 全クリア ----
	public static void ResetAll()
	{
		PlayerPrefs.DeleteKey(KeyPurchased);
		PlayerPrefs.DeleteKey(KeyEquippedMap);
		PlayerPrefs.Save();
	}
	const string KeyEquippedBySceneV1 = "equipped_by_scene_v1";
	[Serializable]
	public class EqBySceneDto
	{
		// sceneId -> (slotId -> itemId)
		public SerializableDictionary<string, SerializableDictionary<string, string>> byScene
				= new SerializableDictionary<string, SerializableDictionary<string, string>>();
	}
	// -------------------------
	// Equipped（シーン別） I/O
	// -------------------------
	public static EqBySceneDto LoadEquippedByScene()
	{
		var raw = PlayerPrefs.GetString(KeyEquippedBySceneV1, "");
		if (string.IsNullOrEmpty(raw)) return new EqBySceneDto();

		try
		{
			return JsonUtility.FromJson<EqBySceneDto>(raw) ?? new EqBySceneDto();
		}
		catch (Exception e)
		{
			Debug.LogError($"[Repo] LoadEquippedByScene parse error: {e}");
			return new EqBySceneDto();
		}
	}

	public static void SaveEquippedByScene(EqBySceneDto dto)
	{
		if (dto == null) dto = new EqBySceneDto();
		PlayerPrefs.SetString(KeyEquippedBySceneV1, JsonUtility.ToJson(dto));
		PlayerPrefs.Save();
	}

	// -------------------------
	// 便利ヘルパ（純関数的：状態は持たない）
	// -------------------------

	/// <summary>
	/// dto から指定シーンのマップを取得。無ければ空を返す（参照は返さない）。
	/// </summary>
	public static SerializableDictionary<string, string> GetEquippedMap(EqBySceneDto dto, string sceneId)
	{
		if (dto == null) return new SerializableDictionary<string, string>();
		if (string.IsNullOrEmpty(sceneId)) sceneId = "default";

		return dto.byScene != null && dto.byScene.TryGetValue(sceneId, out var map) && map != null
				? new SerializableDictionary<string, string>(map) // defensive copy
				: new SerializableDictionary<string, string>();
	}

	/// <summary>
	/// dto に指定シーンのマップをセット（copy-in）。呼び出し側で SaveEquippedByScene(dto) を実行。
	/// </summary>
	public static void SetEquippedMap(EqBySceneDto dto, string sceneId, IReadOnlyDictionary<string, string> map)
	{
		if (dto == null) return;
		if (string.IsNullOrEmpty(sceneId)) sceneId = "default";

		if (dto.byScene == null)
			dto.byScene = new SerializableDictionary<string, SerializableDictionary<string, string>>();

		var copy = new SerializableDictionary<string, string>();
		if (map != null)
		{
			foreach (var kv in map)
				copy[kv.Key ?? ""] = kv.Value ?? "";
		}
		dto.byScene[sceneId] = copy;
	}

	/// <summary>
	/// 1スロットだけ更新した新マップを返す（immutability志向）。呼び出し側で SetEquippedMap→Save。
	/// </summary>
	public static SerializableDictionary<string, string> WithSlot(
			IDictionary<string, string> current,
			string slotId, string itemId,
			string defaultSlot = "avatar/body")
	{
		var result = new SerializableDictionary<string, string>();
		if (current != null)
		{
			foreach (var kv in current)
				result[kv.Key ?? ""] = kv.Value ?? "";
		}

		var sid = string.IsNullOrEmpty(slotId) ? defaultSlot : slotId;
		result[sid] = itemId ?? "";
		return result;
	}

	// （任意）全消去ユーティリティ：開発中のリセット用
	public static void ClearAll()
	{
		PlayerPrefs.DeleteKey(KeyPurchased);
		PlayerPrefs.DeleteKey(KeyEquippedBySceneV1);
		PlayerPrefs.Save();
	}
}
