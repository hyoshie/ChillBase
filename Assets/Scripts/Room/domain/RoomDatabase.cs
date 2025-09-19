using UnityEngine;

[CreateAssetMenu(menuName = "Shop/RoomDatabase")]
public class RoomDatabase : ScriptableObject
{
	public RoomDef[] rooms;

#if UNITY_INCLUDE_TESTS
	public static RoomDatabase Create(params RoomDef[] defs)
	{
		var so = ScriptableObject.CreateInstance<RoomDatabase>();
		so.rooms = defs ?? new RoomDef[0];
		return so;
	}
#endif
}
