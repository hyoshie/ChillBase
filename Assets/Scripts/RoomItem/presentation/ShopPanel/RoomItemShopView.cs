using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomItemShopView : MonoBehaviour
{
	[Header("UI")]
	public Image icon;
	public TextMeshProUGUI priceText;
	public Button actionButton;

	[SerializeField] RoomStateSO roomState;
	[SerializeField] RoomItemServiceSO roomItemService;

	RoomItemDef _data;
	// フィールドでデリゲート保持（解除のため）
	System.Action<System.Collections.Generic.IReadOnlyCollection<string>> _ownedHandler;
	System.Action<string, string> _equippedHandler;


	void OnEnable()
	{
		CurrencyManager.OnCoinsChanged -= Refresh;
		CurrencyManager.OnCoinsChanged += Refresh;

		if (roomState)
		{
			roomState.OnChanged -= OnRoomChanged;
			roomState.OnChanged += OnRoomChanged;
		}
		if (roomItemService && roomItemService.State != null)
		{
			_ownedHandler = _ => Refresh(CurrencyManager.coins);
			_equippedHandler = (slot, id) => Refresh(CurrencyManager.coins);

			roomItemService.State.OnOwnedChanged += _ownedHandler;
			roomItemService.State.OnEquippedChangedBySlot += _equippedHandler;
		}

		Refresh(CurrencyManager.coins);
	}

	void OnDisable()
	{
		CurrencyManager.OnCoinsChanged -= Refresh;
		if (roomState) roomState.OnChanged -= OnRoomChanged;
		if (roomItemService && roomItemService.State != null)
		{
			if (_ownedHandler != null) roomItemService.State.OnOwnedChanged -= _ownedHandler;
			if (_equippedHandler != null) roomItemService.State.OnEquippedChangedBySlot -= _equippedHandler;
			_ownedHandler = null;
			_equippedHandler = null;
		}
	}

	public void Set(RoomItemDef data)
	{
		_data = data;

		if (icon) icon.sprite = data ? data.shopIcon : null;
		if (priceText) priceText.text = data != null ? data.price.ToString("n0") : "";

		if (actionButton)
		{
			actionButton.onClick.RemoveAllListeners();
			actionButton.onClick.AddListener(OnAction);
		}

		Refresh(CurrencyManager.coins);
	}

	void OnRoomChanged(string _) => Refresh(CurrencyManager.coins);

	void Refresh(int _)
	{
		if (_data == null || roomItemService == null) return;

		var roomId = roomState ? roomState.CurrentId : "";
		bool allowed = _data.CanUseIn(roomId);

		if (!allowed)
		{
			if (priceText) priceText.text = "使用不可";
			SetButton("Locked", false);
			return;
		}

		bool owned = roomItemService.IsOwned(_data.id);
		bool equippedAll = owned && roomItemService.IsEquipped(_data);
		bool canUnequip = owned && equippedAll && roomItemService.CanUnequip(_data); // ★ 服なら false

		if (!owned)
		{
			if (priceText) priceText.text = _data.price.ToString("n0");
			SetButton("Buy", CurrencyManager.coins >= _data.price);
			return;
		}

		// 所持済み
		if (equippedAll)
		{
			if (!canUnequip)
			{
				// ★ 服など必須カテゴリ：解除不可をUIで示す
				if (priceText) priceText.text = "Using";
				SetButton("Equipped (Required)", false);
			}
			else
			{
				if (priceText) priceText.text = "Using";
				SetButton("Unequip", true);
			}
		}
		else
		{
			// 未装備 → 装備
			if (priceText) priceText.text = "Owned";
			SetButton("Equip", true);
		}
	}

	void SetButton(string label, bool interactable)
	{
		if (!actionButton) return;
		var t = actionButton.GetComponentInChildren<TextMeshProUGUI>();
		if (t) t.text = label;
		actionButton.interactable = interactable;
		actionButton.gameObject.SetActive(true);
	}

	void OnAction()
	{
		if (_data == null || roomItemService == null) return;

		var roomId = roomState ? roomState.CurrentId : "";
		if (!_data.CanUseIn(roomId)) return;

		bool owned = roomItemService.IsOwned(_data.id);

		if (!owned)
		{
			// 購入→即装備
			if (CurrencyManager.SpendCoins(_data.price))
			{
				if (roomItemService.TryAdd(_data.id))
				{
					roomItemService.Equip(_data);
					Debug.Log($"[Shop] Purchased & Equipped: {_data.id}");
				}
			}
			else
			{
				Debug.Log("[Shop] Not enough coins.");
			}
			Refresh(CurrencyManager.coins);
			return;
		}

		// 所持済み：装備トグル
		bool equippedAll = roomItemService.IsEquipped(_data);

		if (equippedAll)
		{
			// ★ 服など必須カテゴリは解除不可（サービスにも保険あり）
			if (!roomItemService.CanUnequip(_data))
			{
				// 何もしない（UIではボタンは非活性になっている想定）
				Debug.Log($"[Shop] Unequip blocked (required): {_data.id}");
			}
			else
			{
				roomItemService.Unequip(_data);
				Debug.Log($"[Shop] Unequipped: {_data.id}");
			}
		}
		else
		{
			roomItemService.Equip(_data);
			Debug.Log($"[Shop] Equipped: {_data.id}");
		}

		Refresh(CurrencyManager.coins);
	}
}
