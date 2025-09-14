using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/CategoryCatalog", fileName = "ShopCategoryCatalog")]
public class ShopCategoryCatalog : ScriptableObject
{
	[Tooltip("ショップで使用するカテゴリ一覧（表示順は基本 sortOrder → displayName で決めます）")]
	public RoomItemCategoryDef[] categories;

#if UNITY_EDITOR
    // 重複IDやnull混入の軽い検証（エディタのみ）
    void OnValidate()
    {
        if (categories == null) return;

        var seen = new HashSet<string>();
        for (int i = 0; i < categories.Length; i++)
        {
            var c = categories[i];
            if (c == null)
            {
                Debug.LogWarning($"[ShopCategoryCatalog] categories[{i}] が null です。", this);
                continue;
            }
            if (string.IsNullOrEmpty(c.id))
            {
                Debug.LogWarning($"[ShopCategoryCatalog] Category '{c.name}' の id が未設定です。", c);
                continue;
            }
            if (!seen.Add(c.id))
            {
                Debug.LogWarning($"[ShopCategoryCatalog] 重複した id '{c.id}' が見つかりました（'{c.name}'）。", this);
            }
        }
    }
#endif
}
