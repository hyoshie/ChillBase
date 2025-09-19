using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/RoomDef")]
public class RoomDef : ScriptableObject
{
	[Header("ID / 表示")]
	public string id;           // 保存キー。英数字と_のみ推奨
	public string displayName;  // 表示名

	[Header("ショップ表示")]
	public int price;           // コイン価格
	public Sprite icon;         // 一覧用アイコン

	[Header("デフォルト（この部屋に入ったら最初から所持/装備）")]
	public List<RoomItemDef> defaultOwned = new();   // 常に購入済み扱い
	public List<RoomItemDef> defaultEquipped = new();  // 最初に装備される

	// ---- 便利ヘルパ ----

	public IEnumerable<string> GetDefaultOwnedIds()
		=> defaultOwned?.Where(x => x && !string.IsNullOrEmpty(x.id))
						.Select(x => x.id)
						.Distinct()
			 ?? Enumerable.Empty<string>();

	public Dictionary<string, string> BuildDefaultEquippedMap(string fallbackSlot = "avatar/body")
	{
		var map = new Dictionary<string, string>();
		if (defaultEquipped == null) return map;

		foreach (var def in defaultEquipped)
		{
			if (!def || string.IsNullOrEmpty(def.id)) continue;

			var slots = def.GetAllSlots();
			bool any = false;
			if (slots != null)
			{
				foreach (var s in slots)
				{
					var slot = string.IsNullOrEmpty(s) ? fallbackSlot : s;
					if (string.IsNullOrEmpty(slot)) continue;
					map[slot] = def.id;
					any = true;
				}
			}
			if (!any) map[fallbackSlot] = def.id;
		}
		return map;
	}

#if UNITY_INCLUDE_TESTS
	public static RoomDef Create(
		string id,
		IEnumerable<RoomItemDef> defaultOwned = null,
		IEnumerable<RoomItemDef> defaultEquipped = null,
		string displayName = null,
		int price = 0)
	{
		var so = ScriptableObject.CreateInstance<RoomDef>();
		so.id = id;
		so.displayName = displayName ?? id;
		so.price = price;
		so.defaultOwned = defaultOwned != null ? new List<RoomItemDef>(defaultOwned) : new List<RoomItemDef>();
		so.defaultEquipped = defaultEquipped != null ? new List<RoomItemDef>(defaultEquipped) : new List<RoomItemDef>();
		return so;
	}
#endif
}
