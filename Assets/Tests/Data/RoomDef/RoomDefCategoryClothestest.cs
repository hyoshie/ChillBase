#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class RoomDefCategoryClothesTests
{
    // 必要ならここをプロジェクトの運用に合わせて変更
    const string ClothesCategoryId = "clothes";

    static RoomDef[] LoadAllRoomDefs()
    {
        var guids = AssetDatabase.FindAssets("t:RoomDef");
        return guids.Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .Select(p => AssetDatabase.LoadAssetAtPath<RoomDef>(p))
                    .Where(x => x != null)
                    .ToArray();
    }

    static bool IsClothes(RoomItemDef item)
    {
        if (item == null) return false;
        var cat = item.category;
        if (cat == null) return false;
        // id を優先して判定。なければ displayName も一応フォールバック
        var id = (cat.id ?? "").Trim().ToLowerInvariant();
        var dn = (cat.displayName ?? "").Trim().ToLowerInvariant();
        return id == ClothesCategoryId || dn == "clothes";
    }

    [Test]
    public void EveryRoomDef_HasAtLeastOne_Clothes_In_DefaultOwned()
    {
        var rooms = LoadAllRoomDefs();
        Assert.IsNotEmpty(rooms, "RoomDef アセットが見つかりません。");

        var failures = new List<string>();

        foreach (var room in rooms)
        {
            var ok = room.defaultOwned != null &&
                     room.defaultOwned.Any(IsClothes);

            if (!ok) failures.Add(room.name);
        }

        Assert.IsTrue(
            failures.Count == 0,
            "以下の RoomDef に clothes カテゴリの defaultOwned が最低1つありません:\n - " +
            string.Join("\n - ", failures)
        );
    }

    [Test]
    public void EveryRoomDef_HasAtLeastOne_Clothes_In_DefaultEquipped()
    {
        var rooms = LoadAllRoomDefs();
        Assert.IsNotEmpty(rooms, "RoomDef アセットが見つかりません。");

        var failures = new List<string>();

        foreach (var room in rooms)
        {
            var ok = room.defaultEquipped != null &&
                     room.defaultEquipped.Any(IsClothes);

            if (!ok) failures.Add(room.name);
        }

        Assert.IsTrue(
            failures.Count == 0,
            "以下の RoomDef に clothes カテゴリの defaultEquipped が最低1つありません:\n - " +
            string.Join("\n - ", failures)
        );
    }

    [Test]
    public void Clothes_In_DefaultEquipped_Are_Owned_As_Well()
    {
        var rooms = LoadAllRoomDefs();
        Assert.IsNotEmpty(rooms, "RoomDef アセットが見つかりません。");

        var failures = new List<string>();

        foreach (var room in rooms)
        {
            var ownedSet = new HashSet<string>(
                room.defaultOwned?.Where(x => x && !string.IsNullOrEmpty(x.id))
                                  .Select(x => x.id) ?? Enumerable.Empty<string>());

            var missing = room.defaultEquipped?
                .Where(IsClothes)
                .Where(x => string.IsNullOrEmpty(x.id) || !ownedSet.Contains(x.id))
                .Select(x => x ? x.name : "(null)")
                .ToList() ?? new List<string>();

            if (missing.Count > 0)
                failures.Add($"{room.name}: {string.Join(", ", missing)}");
        }

        Assert.IsTrue(
            failures.Count == 0,
            "以下の RoomDef で、defaultEquipped の clothes が defaultOwned に含まれていません:\n - " +
            string.Join("\n - ", failures)
        );
    }
}
#endif
