using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomShopItemView : MonoBehaviour
{
	[Header("Deps")]
	[SerializeField] RoomStateSO roomState;            // ← Inspectorで割り当て
	[SerializeField] RoomShopServiceSO roomService;    // ← さっき作ったSOを割り当て

	[Header("UI")]
	public Image icon;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI priceText;
	public Button actionButton;

	RoomDef _data;

	void OnEnable()
	{
		if (roomState)
		{
			roomState.OnChanged -= OnRoomChanged;
			roomState.OnChanged += OnRoomChanged;
			RefreshByRoom(roomState.CurrentId);
		}
		else
		{
			// フォールバック（SO未設定でも動く）
			RefreshByRoom(roomService.CurrentId);
		}

		CurrencyManager.OnCoinsChanged -= RefreshCoins;
		CurrencyManager.OnCoinsChanged += RefreshCoins;
	}

	void OnDisable()
	{
		if (roomState) roomState.OnChanged -= OnRoomChanged;
		CurrencyManager.OnCoinsChanged -= RefreshCoins;
	}

	public void Set(RoomDef data)
	{
		_data = data;
		if (icon) icon.sprite = data.icon;
		if (nameText) nameText.text = data.displayName;

		if (actionButton)
		{
			actionButton.onClick.RemoveAllListeners();
			actionButton.onClick.AddListener(OnAction);
		}

		// 初期表示
		var cur = roomState.CurrentId;
		RefreshByRoom(cur);
	}

	void OnRoomChanged(string id) => RefreshByRoom(id);
	void RefreshCoins(int _) => RefreshByRoom(roomState.CurrentId);

	void RefreshByRoom(string currentId)
	{
		if (_data == null) return;

		bool owned = roomService.IsOwned(_data.id);

		if (!owned)
		{
			if (priceText) priceText.text = _data.price.ToString("n0");
			SetButton("Buy", interactable: CurrencyManager.coins >= _data.price);
		}
		else
		{
			if (!string.IsNullOrEmpty(currentId) && currentId == _data.id)
			{
				if (priceText) priceText.text = "Using";
				SetButton("Using", interactable: false);
			}
			else
			{
				if (priceText) priceText.text = "Owned";
				SetButton("Use", interactable: true);
			}
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
		if (_data == null || roomService == null) return;

		bool owned = roomService.IsOwned(_data.id);
		if (!owned)
		{
			if (roomService.TryBuy(_data.id, _data.price))
			{
				roomService.Use(_data.id); // 購入直適用
			}
			else
			{
				// コイン不足など
				return;
			}
		}
		else
		{
			if (roomService.CurrentId != _data.id)
				roomService.Use(_data.id);
		}

		// 表示更新
		RefreshByRoom(roomService.CurrentId);
	}
}
