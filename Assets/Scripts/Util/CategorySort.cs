using System.Collections.Generic;
using System.Linq;
public static class CategorySort
{
	// 基本の並び（sortOrder → displayName）
	public static IReadOnlyList<RoomItemCategoryDef> OrderDefault(IEnumerable<RoomItemCategoryDef> cats)
			=> cats.Where(c => c != null)
						 .OrderBy(c => c.sortOrder)
						 .ThenBy(c => c.displayName)
						 .ToList();

	// 簡易オーバーライド：先頭に固定したいカテゴリ（pin）だけ並べ替える
	public static IReadOnlyList<RoomItemCategoryDef> OrderWithPins(
			IEnumerable<RoomItemCategoryDef> cats,
			IEnumerable<RoomItemCategoryDef> pinFirst // 例: Shop画面で最初に出したい順
	)
	{
		var all = OrderDefault(cats).ToList(); // まずは基本順
		if (pinFirst == null) return all;

		var pins = pinFirst.Where(p => p != null).ToList();
		// pins を先頭に、重複を除去しつつ残りを後ろに
		var head = new List<RoomItemCategoryDef>();
		foreach (var p in pins) if (all.Remove(p)) head.Add(p);
		head.AddRange(all);
		return head;
	}

	// もう少し柔軟にしたい場合のフック（場所ごとの簡単な数値オフセット）
	public static IReadOnlyList<RoomItemCategoryDef> OrderWithOffset(
			IEnumerable<RoomItemCategoryDef> cats,
			System.Func<RoomItemCategoryDef, int?> contextOffset // null=オフセットなし
	)
	{
		return cats.Where(c => c != null)
							 .OrderBy(c => (c.sortOrder + (contextOffset?.Invoke(c) ?? 0)))
							 .ThenBy(c => c.displayName)
							 .ToList();
	}
}
