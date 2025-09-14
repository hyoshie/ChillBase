using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/RoomItemState", fileName = "RoomItemState")]
public class RoomItemStateSO : ScriptableObject
{
	[SerializeField] string defaultSlot = "avatar/body";
	public string DefaultSlot => defaultSlot;

	// 内部状態（ランタイム専用）
	HashSet<string> _owned = new();
	Dictionary<string, string> _equippedMap = new();

	// デバッグ用コピー（インスペクタで確認できる）
	[SerializeField, HideInInspector] List<string> debugOwned = new();
	[SerializeField, HideInInspector] List<string> debugEquipped = new();

	public IReadOnlyCollection<string> Owned => _owned;
	public IReadOnlyDictionary<string, string> EquippedMap => _equippedMap;

	// イベント
	public event Action<IReadOnlyCollection<string>> OnOwnedChanged;
	public event Action<string, string> OnEquippedChangedBySlot; // (slotId, itemId|null)
	public event Action OnReset;

	public void SetOwned(HashSet<string> owned)
	{
		owned ??= new HashSet<string>();
		if (_owned.SetEquals(owned)) return;

		_owned = new HashSet<string>(owned);

		// デバッグ用コピー更新
		debugOwned = _owned.ToList();

		OnOwnedChanged?.Invoke(_owned);
	}

	public void SetEquippedMap(Dictionary<string, string> map)
	{
		map ??= new Dictionary<string, string>();

		foreach (var kv in map)
		{
			if (!_equippedMap.TryGetValue(kv.Key, out var cur) || cur != kv.Value)
			{
				_equippedMap[kv.Key] = kv.Value;
				OnEquippedChangedBySlot?.Invoke(kv.Key, kv.Value);
			}
		}
		foreach (var slot in _equippedMap.Keys.ToList())
		{
			if (!map.ContainsKey(slot))
			{
				_equippedMap.Remove(slot);
				OnEquippedChangedBySlot?.Invoke(slot, null);
			}
		}

		// デバッグ用コピー更新
		debugEquipped = _equippedMap.Select(kv => $"{kv.Key}:{kv.Value}").ToList();
	}

	public void SetEquipped(string slotId, string itemIdOrNull)
	{
		slotId ??= "";
		if (string.IsNullOrEmpty(slotId)) return;

		var had = _equippedMap.TryGetValue(slotId, out var cur);
		var next = itemIdOrNull ?? "";

		if (string.IsNullOrEmpty(next))
		{
			if (had)
			{
				_equippedMap.Remove(slotId);
				OnEquippedChangedBySlot?.Invoke(slotId, null);
			}
		}
		else if (!had || cur != next)
		{
			_equippedMap[slotId] = next;
			OnEquippedChangedBySlot?.Invoke(slotId, next);
		}

		// デバッグ用コピー更新
		debugEquipped = _equippedMap.Select(kv => $"{kv.Key}:{kv.Value}").ToList();
	}

	public void Clear()
	{
		var hadOwned = _owned.Count > 0;
		var oldSlots = _equippedMap.Keys.ToList();

		_owned.Clear();
		_equippedMap.Clear();

		debugOwned.Clear();
		debugEquipped.Clear();

		if (hadOwned) OnOwnedChanged?.Invoke(_owned);
		foreach (var s in oldSlots) OnEquippedChangedBySlot?.Invoke(s, null);
		OnReset?.Invoke();
	}
}
