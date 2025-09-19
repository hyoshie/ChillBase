using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 永続化 I/O 専用（状態やイベントは持たない）
/// Backend は Storage 経由で差し替え可能。
/// 既存キーはそのまま使用:
///  - 所持: "purchased_item_ids"
///  - 装備: "equipped_map_v2"
/// </summary>
public static class RoomItemRepository
{
	const string KeyPurchased = "purchased_item_ids";

	public class OwnedDto { public int version = 1; public List<string> items = new(); }

	// -------------------------
	// Owned（購入） I/O
	// -------------------------
	public static OwnedDto LoadOwned()
	{
		var raw = Storage.GetString(KeyPurchased, "");
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
		Storage.SetString(KeyPurchased, JsonUtility.ToJson(dto));
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
		var raw = Storage.GetString(KeyEquippedBySceneV1, "");
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
		Storage.SetString(KeyEquippedBySceneV1, JsonUtility.ToJson(dto));
	}

	// -------------------------
	// 便利ヘルパ（純関数的：状態は持たない）
	// -------------------------
	public static SerializableDictionary<string, string> GetEquippedMap(EqBySceneDto dto, string sceneId)
	{
		if (dto == null) return new SerializableDictionary<string, string>();
		if (string.IsNullOrEmpty(sceneId)) sceneId = "default";

		return dto.byScene != null && dto.byScene.TryGetValue(sceneId, out var map) && map != null
				? new SerializableDictionary<string, string>(map) // defensive copy
				: new SerializableDictionary<string, string>();
	}

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

	public static void ClearAll()
	{
		Storage.DeleteKey(KeyPurchased);
		Storage.DeleteKey(KeyEquippedBySceneV1);
	}
}
