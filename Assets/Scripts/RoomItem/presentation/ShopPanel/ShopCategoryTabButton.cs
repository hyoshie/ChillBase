// ShopCategoryTabButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCategoryTabButton : MonoBehaviour
{
	[SerializeField] Image icon;
	[SerializeField] TMP_Text label;      // Text でも可
	[SerializeField] GameObject selected; // ハイライト用（任意）
	[SerializeField] Button button;

	RoomItemCategoryDef _category;
	System.Action<RoomItemCategoryDef> _onClick;

	public RoomItemCategoryDef Category => _category;

	public void Setup(RoomItemCategoryDef c, System.Action<RoomItemCategoryDef> onClick, bool isSelected)
	{
		_category = c;
		_onClick = onClick;
		if (icon) icon.sprite = c ? c.icon : null;
		if (label) label.text = c ? c.displayName : "All";
		if (button)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => _onClick?.Invoke(_category));
		}
		SetSelected(isSelected);
	}

	public void SetSelected(bool on)
	{
		if (selected) selected.SetActive(on);
	}
}
