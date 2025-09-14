using UnityEngine;
using UnityEngine.UI;

public class RoomShopPanel : MonoBehaviour
{
	[Header("Data")]
	public RoomDatabase database;

	[Header("UI")]
	public GameObject panelRoot;
	public Transform listParent;
	public RoomShopItemView itemViewPrefab;
	public Button closeButton;
	public bool startHidden = true;

	bool _built;
	bool _dirty;

	public bool IsOpen => panelRoot && panelRoot.activeSelf;
	[SerializeField] RoomStateSO roomState; // 追加

	void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Close);
		if (panelRoot && startHidden) panelRoot.SetActive(false);

		roomState.OnChanged += HandleRoomChanged;
	}

	void OnDestroy()
	{
		roomState.OnChanged -= HandleRoomChanged;
	}

	void HandleRoomChanged(string _)
	{
		if (IsOpen) RefreshAllRows();
		else _dirty = true;
	}

	public void Open()
	{
		Debug.Log($"[RoomShopPanel] panelRoot activeSelf={panelRoot.activeSelf} activeInHierarchy={panelRoot.activeInHierarchy} parent={panelRoot.transform.parent?.name}");
		Debug.Log("roomshoppanel open");
		if (!_built || _dirty) { RebuildList(); _dirty = false; }
		Debug.Log("roomshoppanel 2");
		if (panelRoot)
		{

			Debug.Log("roomshoppanel true");
			panelRoot.SetActive(true);
		}
		Debug.Log("roomshoppanel 3");

		// 保険：開いた瞬間に必ず最新表示へ
		RefreshAllRows();
	}

	public void Close()
	{
		if (panelRoot) panelRoot.SetActive(false);
	}

	public void Toggle()
	{
		if (IsOpen) Close(); else Open();
	}

	void BuildList()
	{
		_built = true;
		if (!listParent || !itemViewPrefab || database == null) return;

		foreach (Transform t in listParent) Destroy(t.gameObject);

		var seen = new System.Collections.Generic.HashSet<string>(); // 重複ID防止
		foreach (var def in database.rooms)
		{
			if (def == null || string.IsNullOrEmpty(def.id)) continue;
			if (!seen.Add(def.id)) continue; // 同じidが二重に並ぶと両方Usingの原因
			var view = Instantiate(itemViewPrefab, listParent);
			view.Set(def);
		}
	}

	void RebuildList()
	{
		_built = false;
		BuildList();
	}

	void RefreshAllRows()
	{
		foreach (Transform t in listParent)
		{
			var v = t.GetComponent<RoomShopItemView>();
			if (v) v.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
		}
	}
}
