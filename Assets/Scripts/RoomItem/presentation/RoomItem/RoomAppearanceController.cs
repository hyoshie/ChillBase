using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SlotBinding
{
	public RoomItemSlotDef slot;       // この部屋のSlot
	public RoomItemSlotController controller;

	public string SlotId => slot ? slot.id : "";
}

public class RoomAppearanceController : MonoBehaviour
{
	public string roomId;
	public RoomItemShopCatalog database; // itemId -> RoomItemDef
	public List<SlotBinding> slots = new();

	[Header("ビューPrefabの参照")]
	public StaticSpriteView staticViewPrefab;
	public SpriteAnimView animViewPrefab;
	// ① RoomItemServiceSO を直参照
	[SerializeField] RoomItemServiceSO roomItemService;
	[SerializeField] RoomItemStateSO roomItemState;

	void OnEnable()
	{
		foreach (var s in slots)
			ApplyOne(s, roomItemService.GetEquipped(s.SlotId));

		roomItemState.OnEquippedChangedBySlot += OnEquippedChanged;
	}

	void OnDisable()
	{
		roomItemState.OnEquippedChangedBySlot -= OnEquippedChanged;
	}

	void OnEquippedChanged(string slotId, string itemId)
	{
		var s = slots.Find(x => x.SlotId == slotId);
		if (s != null) ApplyOne(s, itemId);
	}

	void ApplyOne(SlotBinding s, string itemId)
	{
		if (s == null || s.controller == null) return;

		RoomItemDef data = null;
		if (!string.IsNullOrEmpty(itemId) && database != null)
			data = Array.Find(database.items, x => x != null && x.id == itemId);

		if (data == null)
		{
			s.controller.Clear();
			return;
		}

		var visual = data.GetVisualFor(s.SlotId);
		if (visual == null)
		{
			s.controller.Clear();
			return;
		}

		IRoomItemView prefab = visual.viewType switch
		{
			RoomItemViewType.Static => staticViewPrefab,
			RoomItemViewType.Animated => animViewPrefab,
			_ => null
		};

		s.controller.SetView(prefab, visual);
	}
}
