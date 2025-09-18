using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomItemViewType { Static, Animated }

[System.Serializable]
public class RoomItemVisual
{
	public RoomItemSlotDef slot;                   // どのスロットに適用するか
	public RoomItemViewType viewType;              // 静止画かアニメか
	public Sprite sprite;                      // 静止画 or アニメのベース画像
	public RuntimeAnimatorController animator; // アニメ用

	public string SlotId => slot ? slot.id : "";
}

[CreateAssetMenu(menuName = "Shop/Item")]
public class RoomItemDef : ScriptableObject
{
	public string id;      // 外部はこのプロパティで参照

	public string displayName;
	public int price;
	public Sprite shopIcon;
	public RoomItemCategoryDef category;
	public RoomItemVisual[] visuals;

	[Header("Room filter")]
	public List<RoomDef> allowedRooms = new();

	public bool CanUseIn(string roomId)
	{
		if (allowedRooms == null || allowedRooms.Count == 0) return true; // 全許可
		if (string.IsNullOrEmpty(roomId)) return false;
		for (int i = 0; i < allowedRooms.Count; i++)
		{
			var r = allowedRooms[i];
			if (r && r.id == roomId) return true;
		}
		return false;
	}

	// スロットごとのスプライト取得
	public Sprite GetSpriteFor(string slotId)
	{
		if (visuals != null)
		{
			foreach (var v in visuals)
				if (v != null && v.SlotId == slotId)
					return v.sprite;
		}
		return null;
	}

	// SlotDef を直接渡せるオーバーロード
	public Sprite GetSpriteFor(RoomItemSlotDef slotDef) =>
			slotDef ? GetSpriteFor(slotDef.id) : null;

	// このアイテムが使うすべてのslot列挙
	public IEnumerable<string> GetAllSlots()
	{
		if (visuals != null && visuals.Length > 0)
		{
			foreach (var v in visuals)
				if (v != null && !string.IsNullOrEmpty(v.SlotId))
					yield return v.SlotId;
		}
	}

	// 文字列IDが必要な場面向けのヘルパー
	public IReadOnlyList<string> AllowedRoomIds =>
			allowedRooms?.Where(r => r).Select(r => r.id).ToList() ?? new List<string>();

	// 特定スロット用の見た目を取得
	public RoomItemVisual GetVisualFor(string slotId)
	{
		if (visuals == null) return null;
		return System.Array.Find(visuals, v => v != null && v.SlotId == slotId);
	}

	public bool RequiresAlwaysEquipped() =>
		category && category.requiresAlwaysEquipped;

#if UNITY_EDITOR
	void OnValidate()
	{
		if (category == null)
		{
			// Project内の任意のカテゴリアセットを探して自動で設定
			category = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomItemCategoryDef>(
					"Assets/Data/Shop/ItemShop/ItemCategories/Decor.asset"
			);
		}
	}
#endif
}
