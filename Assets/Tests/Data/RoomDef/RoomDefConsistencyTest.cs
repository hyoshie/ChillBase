#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class RoomDefConsistencyTests
{
	static RoomDef[] LoadAllRoomDefs()
	{
		var guids = AssetDatabase.FindAssets("t:RoomDef");
		return guids
				.Select(g => AssetDatabase.GUIDToAssetPath(g))
				.Select(p => AssetDatabase.LoadAssetAtPath<RoomDef>(p))
				.Where(x => x != null)
				.ToArray();
	}

	[Test]
	public void DefaultEquipped_IsSubsetOf_DefaultOwned()
	{
		var rooms = LoadAllRoomDefs();
		Assert.IsNotEmpty(rooms, "RoomDef アセットが見つかりません。");

		foreach (var room in rooms)
		{
			var ownedIds = room.defaultOwned?
					.Where(x => x && !string.IsNullOrEmpty(x.id))
					.Select(x => x.id)
					.Distinct()
					.ToHashSet() ?? new System.Collections.Generic.HashSet<string>();

			var missing = room.defaultEquipped?
					.Where(x => x && !string.IsNullOrEmpty(x.id))
					.Select(x => x.id)
					.Where(id => !ownedIds.Contains(id))
					.Distinct()
					.ToList() ?? new System.Collections.Generic.List<string>();

			Assert.IsTrue(
					missing.Count == 0,
					$"[{room.name}] defaultEquipped に defaultOwned に含まれないIDがあります: {string.Join(", ", missing)}"
			);
		}
	}

	[Test]
	public void DefaultLists_NoNullRefs_And_NoEmptyIds()
	{
		var rooms = LoadAllRoomDefs();
		foreach (var room in rooms)
		{
			// null 参照は無い方針（もし許容したいならコメントアウト）
			var nullInOwned = room.defaultOwned?.Any(x => x == null) ?? false;
			var nullInEquipped = room.defaultEquipped?.Any(x => x == null) ?? false;
			Assert.IsFalse(nullInOwned, $"[{room.name}] defaultOwned に null 参照があります。");
			Assert.IsFalse(nullInEquipped, $"[{room.name}] defaultEquipped に null 参照があります。");

			// id 未設定も弾く（運用に合わせて厳しさ調整可）
			var emptyIdInOwned = room.defaultOwned?
					.Where(x => x != null)
					.Any(x => string.IsNullOrEmpty(x.id)) ?? false;
			var emptyIdInEq = room.defaultEquipped?
					.Where(x => x != null)
					.Any(x => string.IsNullOrEmpty(x.id)) ?? false;

			Assert.IsFalse(emptyIdInOwned, $"[{room.name}] defaultOwned に id 未設定の Item が含まれます。");
			Assert.IsFalse(emptyIdInEq, $"[{room.name}] defaultEquipped に id 未設定の Item が含まれます。");
		}
	}

	[Test]
	public void BuildDefaultEquippedMap_DoesNotCreate_UnknownSlots()
	{
		var rooms = LoadAllRoomDefs();
		foreach (var room in rooms)
		{
			var map = room.BuildDefaultEquippedMap("avatar/body");
			Assert.IsNotNull(map);
			// 値は空文字でない（=装備する場合は必ず itemId が入る）
			Assert.IsFalse(map.Values.Any(v => string.IsNullOrEmpty(v)),
					$"[{room.name}] BuildDefaultEquippedMap に空の itemId が含まれています。");
		}
	}
}
#endif
