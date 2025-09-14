using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ItemCategory", fileName = "RoomItemCategory")]
public class RoomItemCategoryDef : ScriptableObject
{
	public string id;          // 永続用キー（不変）
	public string displayName; // 表示名
	public Sprite icon;        // 任意
	public int sortOrder = 0;  // ★ 基本の並び順（小さいほど前）
														 // 複雑になるならSOで制御
}
