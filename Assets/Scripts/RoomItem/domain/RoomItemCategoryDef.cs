using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ItemCategory", fileName = "RoomItemCategory")]
public class RoomItemCategoryDef : ScriptableObject
{
	public string id;          // 永続用キー（不変）
	public string displayName; // 表示名
	public Sprite icon;        // 任意
	public bool requiresAlwaysEquipped;
	public int sortOrder = 0;  // ★ 基本の並び順（小さいほど前）
														 // 複雑になるならSOで制御
#if UNITY_INCLUDE_TESTS
	public static RoomItemCategoryDef Create(string id, bool requiresAlwaysEquipped = false, string displayName = null)
	{
		var so = ScriptableObject.CreateInstance<RoomItemCategoryDef>();
		so.id = id;
		so.requiresAlwaysEquipped = requiresAlwaysEquipped;
		so.displayName = displayName ?? id;
		return so;
	}
#endif
}
