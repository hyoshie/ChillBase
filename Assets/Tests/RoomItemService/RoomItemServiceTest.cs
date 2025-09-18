using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RoomItemServiceTests
{
	RoomItemServiceSO service;
	RoomItemStateSO state;
	RoomStateSO roomState;
	RoomItemDef clothesItem;
	RoomItemCategoryDef clothesCat;
	RoomItemShopCatalog catalog;
	RoomDatabase roomDatabase;

	[SetUp]
	public void Setup()
	{
		// ScriptableObject生成
		LogAssert.ignoreFailingMessages = true;
		state = ScriptableObject.CreateInstance<RoomItemStateSO>();
		roomState = ScriptableObject.CreateInstance<RoomStateSO>();
		service = ScriptableObject.CreateInstance<RoomItemServiceSO>();
		LogAssert.ignoreFailingMessages = false;
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
		roomDatabase = ScriptableObject.CreateInstance<RoomDatabase>();

		service.InjectForTests(state, roomState, roomDatabase, catalog);

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
		Object.DestroyImmediate(roomDatabase);
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
	[Test]
	public void Clothes_CanSwitchToAnotherClothes_ByEquipReplacement()
	{
		// Arrange: 服Bを用意して、所持 & カタログに追加
		var clothesItemB = ScriptableObject.CreateInstance<RoomItemDef>();
		clothesItemB.id = "shirt002";
		clothesItemB.category = clothesCat; // 同じ「clothes」カテゴリ

		// カタログ更新（A,B 両方を引けるように）
		catalog.items = new[] { clothesItem, clothesItemB };

		// 所持に追加（A は Setup で所持済み・装備済み）
		var owned = new System.Collections.Generic.HashSet<string>(state.Owned) { clothesItemB.id };
		state.SetOwned(owned);

		// Sanity: いまは A が装備されているはず
		Assert.AreEqual(clothesItem.id, service.GetEquipped("avatar/body"));

		// Act: 服Bに装備切り替え（Unequip は呼ばず、Equip で置き換える）
		bool changed = service.Equip(clothesItemB);

		// Assert: 置き換え成功（true が返り、スロットの中身が B になる）
		Assert.IsTrue(changed, "Clothes → Clothes の置き換え Equip は成功すべき");
		Assert.AreEqual(clothesItemB.id, service.GetEquipped("avatar/body"), "装備は shirt002 に置き換わる");

		// 追加確認：B は装備扱い / A は非装備
		Assert.IsTrue(service.IsEquipped(clothesItemB));
		Assert.IsFalse(service.IsEquipped(clothesItem));

		// 後片付け
		Object.DestroyImmediate(clothesItemB);
	}
}
