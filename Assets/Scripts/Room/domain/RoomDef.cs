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

	// ---- 便利ヘルパ（まだ Service からは呼ばない） ----

	// 所持ID一覧を重複なしで返す
	public IEnumerable<string> GetDefaultOwnedIds()
			=> defaultOwned?.Where(x => x && !string.IsNullOrEmpty(x.id))
											.Select(x => x.id)
											.Distinct()
				 ?? Enumerable.Empty<string>();

	// 初期装備マップ（slotId -> itemId）を組み立て（後勝ち）
	public Dictionary<string, string> BuildDefaultEquippedMap(string fallbackSlot = "avatar/body")
	{
		var map = new Dictionary<string, string>();
		if (defaultEquipped == null) return map;

		foreach (var def in defaultEquipped)
		{
			if (!def || string.IsNullOrEmpty(def.id)) continue;

			// そのアイテムが使う全スロット（なければ fallbackSlot）
			var slots = def.GetAllSlots();
			bool any = false;
			if (slots != null)
			{
				foreach (var s in slots)
				{
					var slot = string.IsNullOrEmpty(s) ? fallbackSlot : s;
					if (string.IsNullOrEmpty(slot)) continue;
					map[slot] = def.id; // 同一スロットに複数あれば最後のものが採用
					any = true;
				}
			}
			if (!any) map[fallbackSlot] = def.id;
		}
		return map;
	}
}
