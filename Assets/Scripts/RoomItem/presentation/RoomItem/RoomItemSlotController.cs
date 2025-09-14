using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class RoomItemSlotController : MonoBehaviour
{
	[Tooltip("未設定なら自動で自分自身の RectTransform が使われます")]
	public RectTransform viewRoot;

	private IRoomItemView _current;

	// エディタでアタッチ直後 & Reset メニュー実行時に呼ばれる
	void Reset()
	{
		viewRoot = GetComponent<RectTransform>();
	}

	// インスペクタ変更時に呼ばれる（常に自己修復）
	void OnValidate()
	{
		if (viewRoot == null)
			viewRoot = GetComponent<RectTransform>();
	}

	public void SetView(IRoomItemView prefab, RoomItemVisual visual)
	{
		var parent = viewRoot != null ? viewRoot : GetComponent<RectTransform>();

		// 簡易チェック：UI親じゃないと避難する可能性を警告
		var canvas = parent.GetComponentInParent<Canvas>();
		if (canvas == null)
			Debug.LogWarning($"[SlotController] Parent '{parent.name}' is not under a Canvas. UI may be reparented.", this);

		// 既存を破棄
		if (_current != null)
		{
			_current.Hide();
			Destroy((_current as Component).gameObject);
			_current = null;
		}

		if (prefab == null || visual == null) return;

		var instGO = Instantiate(((Component)prefab).gameObject, parent);
		_current = instGO.GetComponent<IRoomItemView>();
		_current.Show(visual);
	}

	public void Clear()
	{
		if (_current != null)
		{
			_current.Hide();
			Destroy((_current as Component).gameObject);
			_current = null;
		}
	}

	// 右クリックメニューからも手動で自己設定できる
	[ContextMenu("Use Self As ViewRoot")]
	void UseSelfAsViewRoot()
	{
		viewRoot = GetComponent<RectTransform>();
	}
}
