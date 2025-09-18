using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

/// <summary>
/// Repository ⇄ State の仲介。装備/解除/購入のルール適用。
/// 装備はシーン別に保存/復元（RoomStateSO.CurrentId）
/// 保存タイミング：購入・装備・解除で即保存
/// </summary>
[CreateAssetMenu(menuName = "Game/RoomItemService", fileName = "RoomItemService")]
public partial class RoomItemServiceSO : ScriptableObject
{
	[Header("States")]
	[SerializeField] RoomItemStateSO state = null!;     // 現在シーンの装備ビュー＋Owned
	public RoomItemStateSO State => state; // 読み取り専用で公開
																				 // これ消して移行したい。とりあえず
	[SerializeField] RoomStateSO roomState = null!;     // 現在シーンIDの単一ソース

	// ★追加：RoomDef からデフォルト装備を参照するため
	[SerializeField] RoomDatabase roomDatabase = null!;
	// 服カテゴリー判定のために、全アイテムを引けるカタログを割り当てる
	[SerializeField] RoomItemShopCatalog shopCatalog = null!; // items: RoomItemDef[] を持つ想定

	// stateless Repository のDTO（起動中キャッシュはService側で保持）
	RoomItemRepository.OwnedDto _ownedDto = null!;
	RoomItemRepository.EqBySceneDto _eqDto = null!;

	void OnEnable()
	{
		if (!state || !roomDatabase || !shopCatalog)
		{
			Debug.LogError($"{nameof(RoomItemServiceSO)}: Serialized refs are not set.");
		}
		if (roomState != null)
		{
			roomState.OnChanged -= HandleRoomChanged;
			roomState.OnChanged += HandleRoomChanged;
		}
	}

	void OnDisable()
	{
		if (roomState != null) roomState.OnChanged -= HandleRoomChanged;
	}

	// ---- 起動・同期 ----
	public void ReloadAllFromStorage()
	{
		_ownedDto = RoomItemRepository.LoadOwned();
		_eqDto = RoomItemRepository.LoadEquippedByScene();

		var sceneId = roomState ? roomState.CurrentId : "default";
		SyncScene(sceneId, forceDefaults: false, wipeAll: false);

	}

	public void SaveAll()
	{
		// Owned保存
		RoomItemRepository.SaveOwned(state.Owned);

		// 現在シーンの装備を byScene に反映して保存
		var sceneId = roomState ? roomState.CurrentId : "default";
		RoomItemRepository.SetEquippedMap(_eqDto, sceneId, state.EquippedMap);
		RoomItemRepository.SaveEquippedByScene(_eqDto);
	}

	public void ResetAll()
	{
		state.Clear();
		var sceneId = roomState ? roomState.CurrentId : "default";
		// 全削除して、現在シーンのデフォルトを適用して保存し直す
		SyncScene(sceneId, forceDefaults: true, wipeAll: true);
	}

	// ---- シーン切替：装備ビュー差し替え（デフォルト適用もここで） ----
	void HandleRoomChanged(string newSceneId)
	{
		// シーン切替時も同じ（保存があれば尊重、無ければデフォルト適用）
		SyncScene(newSceneId, forceDefaults: false, wipeAll: false);
	}

	// 共通：指定sceneIdで Owned/Equipped を統合→State反映→保存
	private void SyncScene(string sceneId, bool forceDefaults = false, bool wipeAll = false)
	{
		// wipe指定なら全削除してDTO再生成
		if (wipeAll)
		{
			RoomItemRepository.ClearAll();
			_ownedDto = new RoomItemRepository.OwnedDto();
			_eqDto = new RoomItemRepository.EqBySceneDto();
		}

		// 既存DTOが無ければロード
		_ownedDto ??= RoomItemRepository.LoadOwned();
		_eqDto ??= RoomItemRepository.LoadEquippedByScene();

		// 既存保存の読み出し（forceDefaultsなら“保存なし”扱いにする）
		var ownedIn = new HashSet<string>(_ownedDto.items ?? new List<string>());
		var savedMap = (!forceDefaults) ? RoomItemRepository.GetEquippedMap(_eqDto, sceneId)
																		: new Dictionary<string, string>(); // 空＝初回扱い

		// 初期状態の解決（stateは触らない純粋関数）
		var result = ResolveInitialState(sceneId, ownedIn, savedMap);

		// Stateへ一度だけ反映
		state.SetOwned(result.owned);
		state.SetEquippedMap(result.equipped);

		// 変更分の保存
		if (wipeAll || result.saveOwned)
		{
			_ownedDto.items = result.owned.ToList();
			RoomItemRepository.SaveOwned(_ownedDto.items);
		}
		if (wipeAll || result.saveEquipped)
		{
			RoomItemRepository.SetEquippedMap(_eqDto, sceneId, result.equipped);
			RoomItemRepository.SaveEquippedByScene(_eqDto);
		}
	}

	// ---- 所持（Owned）----
	public bool IsOwned(string itemId)
		=> !string.IsNullOrEmpty(itemId) && state.Owned.Contains(itemId);

	/// <summary>購入（所持追加）→ 即保存</summary>
	public bool TryAdd(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return false;
		if (state.Owned.Contains(itemId)) return false;

		// State更新
		var next = new HashSet<string>(state.Owned) { itemId };
		state.SetOwned(next);

		// DTO更新＋保存
		if (_ownedDto == null) _ownedDto = new RoomItemRepository.OwnedDto();
		_ownedDto.items = state.Owned.ToList();
		RoomItemRepository.SaveOwned(_ownedDto.items);
		return true;
	}

	// ---- 装備（Equipped：シーン別）----
	public string? GetEquipped(string slotId)
	{
		if (string.IsNullOrEmpty(slotId)) slotId = state.DefaultSlot;
		return state.EquippedMap.TryGetValue(slotId, out var itemId) ? itemId : null;
	}

	bool TryEquip(string slotId, string itemId)
	{
		if (string.IsNullOrEmpty(slotId)) slotId = state.DefaultSlot;
		if (string.IsNullOrEmpty(slotId) || string.IsNullOrEmpty(itemId)) return false;
		if (!IsOwned(itemId)) return false; // 未所持は装備不可

		var cur = GetEquipped(slotId);
		if (cur == itemId) return false; // 変化なし

		// State 更新
		state.SetEquipped(slotId, itemId);

		// Repository 保存（現在シーン）
		var sceneId = roomState ? roomState.CurrentId : "default";
		RoomItemRepository.SetEquippedMap(_eqDto, sceneId, state.EquippedMap);
		RoomItemRepository.SaveEquippedByScene(_eqDto);
		return true;
	}


	bool TryUnequip(string slotId)
	{
		if (string.IsNullOrEmpty(slotId)) slotId = state.DefaultSlot;
		if (string.IsNullOrEmpty(slotId)) return false;

		var cur = GetEquipped(slotId);
		if (string.IsNullOrEmpty(cur)) return false; // もともと未装備

		// ★ ここで服解除を禁止
		var curDef = FindItemDef(cur);
		if (curDef == null || curDef.RequiresAlwaysEquipped())
			return false;

		state.SetEquipped(slotId, null);
		var sceneId = roomState ? roomState.CurrentId : "default";
		RoomItemRepository.SetEquippedMap(_eqDto, sceneId, state.EquippedMap);
		RoomItemRepository.SaveEquippedByScene(_eqDto);
		return true;
	}

	// ItemDef → 複数スロット解決
	IEnumerable<string> ResolveSlots(RoomItemDef item)
	{
		if (item == null) yield break;

		var slots = item.GetAllSlots();
		bool any = false;
		if (slots != null)
		{
			var set = new HashSet<string>();
			foreach (var s in slots)
			{
				var sid = string.IsNullOrEmpty(s) ? state.DefaultSlot : s;
				if (!string.IsNullOrEmpty(sid) && set.Add(sid))
				{
					any = true;
					yield return sid;
				}
			}
		}
		if (!any) yield return state.DefaultSlot;
	}

	// ---- 高レベルAPI（Item単位）----
	public bool Equip(RoomItemDef item)
	{
		if (item == null) return false;
		var itemId = item.id;
		if (string.IsNullOrEmpty(itemId)) return false;
		if (!IsOwned(itemId)) return false; // 未所持は装備不可

		bool changed = false;
		foreach (var slot in ResolveSlots(item))
			changed |= TryEquip(slot, itemId);

		return changed;
	}

	public bool Unequip(RoomItemDef item)
	{
		if (item == null || item.RequiresAlwaysEquipped
		())
		{
			return false;
		}

		bool changed = false;
		foreach (var slot in ResolveSlots(item))
			changed |= TryUnequip(slot);

		return changed;
	}

	public bool IsEquipped(RoomItemDef item)
	{
		if (item == null) return false;
		var itemId = item.id;
		if (string.IsNullOrEmpty(itemId)) return false;

		foreach (var rawSlot in ResolveSlots(item))
		{
			var slot = string.IsNullOrEmpty(rawSlot) ? state.DefaultSlot : rawSlot;
			var cur = GetEquipped(slot);
			if (cur != itemId) return false;
		}
		return true;
	}

	// ========== ここから：デフォルト適用の純粋ロジック ==========
	struct InitResolveResult
	{
		public HashSet<string> owned;
		public Dictionary<string, string> equipped;
		public bool saveOwned;
		public bool saveEquipped;
	}

	InitResolveResult ResolveInitialState(
		string sceneId,
		HashSet<string> ownedIn,
		IDictionary<string, string> savedEquippedOrNull)
	{
		var owned = new HashSet<string>(ownedIn ?? new HashSet<string>());
		var equipped = new Dictionary<string, string>(savedEquippedOrNull ?? new Dictionary<string, string>());

		bool hasSavedEquipped = savedEquippedOrNull != null && savedEquippedOrNull.Count > 0;
		bool saveOwned = false;
		bool saveEquipped = false;

		var room = FindRoomDef(sceneId);
		if (room != null)
		{
			// 1) Owned 補強：defaultOwned ∪ defaultEquipped を常に所持に含める
			var mustOwnIds =
				(room.GetDefaultOwnedIds() ?? Enumerable.Empty<string>())
				.Concat(room.defaultEquipped?
					.Where(x => x && !string.IsNullOrEmpty(x.id))
					.Select(x => x.id)
					.Distinct() ?? Enumerable.Empty<string>());

			foreach (var id in mustOwnIds)
				if (!string.IsNullOrEmpty(id) && owned.Add(id)) saveOwned = true;

			// 2) 装備：初回（保存なし）のみ defaultEquipped を適用
			if (!hasSavedEquipped)
			{
				var defMap = room.BuildDefaultEquippedMap(state.DefaultSlot)
							 ?? new Dictionary<string, string>();
				equipped = new Dictionary<string, string>(defMap);
				saveEquipped = equipped.Count > 0;
			}
		}

		return new InitResolveResult
		{
			owned = owned,
			equipped = equipped,
			saveOwned = saveOwned,
			saveEquipped = saveEquipped,
		};
	}

	RoomDef? FindRoomDef(string sceneId)
	{
		if (roomDatabase == null || string.IsNullOrEmpty(sceneId)) return null;
		if (roomDatabase.rooms == null) return null;
		for (int i = 0; i < roomDatabase.rooms.Length; i++)
		{
			var r = roomDatabase.rooms[i];
			if (r && r.id == sceneId) return r;
		}
		return null;
	}

	RoomItemDef? FindItemDef(string itemId)
	{
		if (string.IsNullOrEmpty(itemId) || shopCatalog == null || shopCatalog.items == null)
		{
			Debug.LogWarning("FindItemDef: property is not set.");
			return null;
		}
		for (int i = 0; i < shopCatalog.items.Length; i++)
		{
			var def = shopCatalog.items[i];
			if (def && def.id == itemId) return def;
		}
		Debug.LogWarning("FindItemDef: Not Found");
		return null;
	}
}
