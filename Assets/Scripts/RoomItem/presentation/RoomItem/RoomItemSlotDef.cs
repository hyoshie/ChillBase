// using UnityEngine;

// [CreateAssetMenu(menuName = "Game/ItemSlotDef")]
// public class ItemSlotDef : ScriptableObject
// {
// 	public string id;           // 例: "top", "bottom"
// 															// public string displayName;  // Inspector/表示用（任意）
// }

using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemSlotDef")]
public class RoomItemSlotDef : ScriptableObject
{
	public string id;     // 外から参照用にプロパティ化しておく
}
