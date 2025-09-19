using UnityEngine;

[CreateAssetMenu(menuName = "Shop/RoomItemCatalog")]
public class RoomItemShopCatalog : ScriptableObject
{
	public RoomItemDef[] items;

#if UNITY_INCLUDE_TESTS
	public static RoomItemShopCatalog Create(params RoomItemDef[] defs)
	{
		var so = ScriptableObject.CreateInstance<RoomItemShopCatalog>();
		so.items = defs ?? new RoomItemDef[0];
		return so;
	}
#endif
}
