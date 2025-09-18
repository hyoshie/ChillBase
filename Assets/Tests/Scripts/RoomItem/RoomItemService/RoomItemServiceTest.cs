using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
// using System; // IDisposable が必要なら

public class RoomItemServiceTests
{
	// ★ InMemory ストアのスコープ（Setupで入替、TearDownで復帰）
	System.IDisposable _storeScope;

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
		// 1) まずストアを InMemory に（テスト間を完全分離）
		_storeScope = Storage.PushInMemoryScope("TEST_RoomItemService_");

		// 2) ScriptableObject 生成
		LogAssert.ignoreFailingMessages = true;
		state = ScriptableObject.CreateInstance<RoomItemStateSO>();
		roomState = ScriptableObject.CreateInstance<RoomStateSO>();
		service = ScriptableObject.CreateInstance<RoomItemServiceSO>();
		LogAssert.ignoreFailingMessages = false;

		// 3) ルームID初期化
		roomState.Set("TestRoom");

		// 4) 服カテゴリ & アイテム
		clothesCat = RoomItemCategoryDef.Create(id: "clothes", requiresAlwaysEquipped: true);
		clothesItem = RoomItemDef.Create(id: "shirt001", category: clothesCat);


		// 5) サービス用カタログ/DB
		catalog = RoomItemShopCatalog.Create(defs: new[] { clothesItem });
		roomDatabase = RoomDatabase.Create();

		// 6) 依存注入
		service.InjectForTests(state, roomState, roomDatabase, catalog);

		// 7) Repository DTO を初期化（InMemoryから空読み込み）
		service.ReloadAllFromStorage();

		// 8) 所持 & 装備を直接セット（テストの初期状態）
		state.SetOwned(new System.Collections.Generic.HashSet<string> { clothesItem.id });
		state.SetEquipped("avatar/body", clothesItem.id);
	}

	[TearDown]
	public void Teardown()
	{
		// ストア復帰（InMemoryを破棄）
		_storeScope?.Dispose();

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
		var clothesItemB = RoomItemDef.Create(id: "shirt002", category: clothesCat);

		// カタログ更新（A,B 両方を引けるように）
		catalog.items = new[] { clothesItem, clothesItemB };

		// 所持に追加（A は Setup で所持済み・装備済み）
		var owned = new HashSet<string>(state.Owned) { clothesItemB.id };
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

	// [Test]
	// public void Service_FirstEnter_AppliesDefaults_ThenNotOverride()
	// {

	// 	// テスト用シーンID（カタログ/設定の default* が定義されている想定）
	// 	// const string sceneX = "scene_test_defaults";
	// 	string sceneX = RoomConstants.DefaultRoomId;
	// 	roomState.Set(sceneX);


	// 	// Act (1): 初回ロード -----------------------------------------------
	// 	service.ReloadAllFromStorage(); // 初回入室相当
	// 	Debug.Log($"reload");
	// 	foreach (var owned in state.Owned)
	// 	{
	// 		Debug.Log($"owendId:{owned}");
	// 	}

	// 	// Assert (1): デフォルトが適用される
	// 	// defaultOwned が Owned に含まれる / defaultEquipped が反映される
	// 	var defaults = service.ResolveInitialState(sceneX, new HashSet<string>(new List<string>()), new Dictionary<string, string>()); // { ownedIds, equippedMap } を返す想定
	// 	Debug.Log($"reslove");
	// 	foreach (var owned in state.Owned)
	// 	{
	// 		Debug.Log($"owendId:{owned}");
	// 	}
	// 	foreach (var id in defaults.owned)
	// 	{
	// 		Debug.Log($"id: {id}");
	// 		Assert.IsTrue(service.IsOwned(id),
	// 			$"初回ロード時、defaultOwned の {id} は Owned に含まれるべき");
	// 	}
	// 	foreach (var kv in defaults.equipped) // kv.Key: slot, kv.Value: id
	// 	{
	// 		Assert.AreEqual(kv.Value, service.GetEquipped(kv.Key),
	// 			$"初回ロード時、スロット {kv.Key} は {kv.Value} が装備されるべき");
	// 	}

	// 	// ユーザー操作: デフォルトを一部変更して保存される状態を作る
	// 	// 例: 別の服アイテムを作って Equip（服は「解除禁止」仕様なので置換で変更）
	// 	// 例: 別の服アイテムを作って Equip（服は「解除禁止」仕様なので置換で変更）
	// 	var userChosen = ScriptableObject.CreateInstance<RoomItemDef>();
	// 	userChosen.id = "user_choice_shirtX";
	// 	userChosen.displayName = "User Choice Shirt X";
	// 	userChosen.category = clothesCat; // 服カテゴリ

	// 	var slot = ScriptableObject.CreateInstance<RoomItemSlotDef>();
	// 	slot.id = "sample_slot";
	// 	// Visual を追加
	// 	var visual = new RoomItemVisual
	// 	{
	// 		slot = slot,
	// 		viewType = RoomItemViewType.Static, // 静止画タイプ
	// 		sprite = null,                      // 必要ならテスト用の Sprite を割り当て
	// 		animator = null
	// 	};
	// 	userChosen.visuals = new[] { visual };
	// 	Debug.LogWarning($"before assert: {state.Owned.Count}");


	// 	// 所有 → 装備（装備時に保存が呼ばれる想定）
	// 	Assert.IsTrue(service.TryAdd(userChosen.id),
	// 		"ユーザー選択アイテムを Owned に追加できるはず");
	// 	Assert.IsTrue(service.Equip(userChosen),
	// 		"ユーザー選択アイテムを装備できるはず");
	// 	// var changedOwnedSnapshot = state.Owned;
	// 	// var changedEquippedSnapshot = state.EquippedMap;

	// 	// // // Act (2): 2回目以降のロード（保存あり） ----------------------------
	// 	// service.ReloadAllFromStorage();
	// 	// Debug.Log($"snap:{changedOwnedSnapshot.Count}");
	// 	// Debug.Log($"snap:{changedEquippedSnapshot.Count}");

	// 	// Assert (2): ユーザーの保存内容が優先され、デフォルトで上書きされない
	// 	// ＝ 直前のスナップショットと同一であること
	// 	// CollectionAssert.AreEquivalent(
	// 	// 	changedOwnedSnapshot,
	// 	// 	state.Owned,
	// 	// 	"2回目以降は Owned がデフォルトで上書きされてはならない"
	// 	// );
	// 	// var afterEquipped = state.EquippedMap;
	// 	// Assert.AreEqual(
	// 	// 	changedEquippedSnapshot.Count, afterEquipped.Count,
	// 	// 	"2回目以降は EquippedMap のスロット数が変わらないはず"
	// 	// );
	// 	// foreach (var kv in changedEquippedSnapshot)
	// 	// {
	// 	// 	Assert.IsTrue(afterEquipped.TryGetValue(kv.Key, out var id),
	// 	// 		$"2回目以降、スロット {kv.Key} は保持されるべき");
	// 	// 	Assert.AreEqual(kv.Value, id,
	// 	// 		$"2回目以降、スロット {kv.Key} はユーザー保存の {kv.Value} のまま");
	// 	// }

	// 	// // // Cleanup ------------------------------------------------------------
	// 	if (userChosen) Object.DestroyImmediate(userChosen);
	// }
}
