using NUnit.Framework;
using UnityEngine;

public class RoomItemServiceTests
{
	RoomItemServiceSO service;
	RoomItemStateSO state;
	RoomStateSO roomState;
	RoomItemDef clothesItem;
	RoomItemCategoryDef clothesCat;
	RoomItemShopCatalog catalog;

	[SetUp]
	public void Setup()
	{
		// ScriptableObject生成
		state = ScriptableObject.CreateInstance<RoomItemStateSO>();
		roomState = ScriptableObject.CreateInstance<RoomStateSO>();
		service = ScriptableObject.CreateInstance<RoomItemServiceSO>();

		// フィールド割り当て（privateなので反射）
		typeof(RoomItemServiceSO)
				.GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(service, state);
		typeof(RoomItemServiceSO)
				.GetField("roomState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(service, roomState);

		// ルームID初期化
		roomState.Set("TestRoom");

		// 服カテゴリ & アイテム
		clothesCat = ScriptableObject.CreateInstance<RoomItemCategoryDef>();
		clothesCat.id = "clothes";
		clothesCat.requiresAlwaysEquipped = true;

		clothesItem = ScriptableObject.CreateInstance<RoomItemDef>();
		clothesItem.id = "shirt001";
		clothesItem.category = clothesCat;

		// ---- ★ここが追加：サービス用のカタログを設定 ----
		catalog = ScriptableObject.CreateInstance<RoomItemShopCatalog>();
		catalog.items = new[] { clothesItem };
		typeof(RoomItemServiceSO)
				.GetField("shopCatalog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(service, catalog);
		// ---------------------------------------------

		// 所持 & 装備状態を直接設定
		state.SetOwned(new System.Collections.Generic.HashSet<string> { clothesItem.id });
		state.SetEquipped("avatar/body", clothesItem.id);
	}

	[TearDown]
	public void Teardown()
	{
		Object.DestroyImmediate(service);
		Object.DestroyImmediate(state);
		Object.DestroyImmediate(roomState);
		Object.DestroyImmediate(clothesItem);
		Object.DestroyImmediate(clothesCat);
		Object.DestroyImmediate(catalog);
	}

	[Test]
	public void Clothes_CannotBeUnequipped()
	{
		// Act
		bool result = service.Unequip(clothesItem);

		// Assert
		Assert.IsFalse(result, "服カテゴリは Unequip が成功してはいけない");
		Assert.AreEqual(clothesItem.id, state.EquippedMap["avatar/body"], "服カテゴリは装備が外れず維持される");
	}
}
