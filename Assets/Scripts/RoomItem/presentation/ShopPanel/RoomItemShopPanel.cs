// RoomItemShopPanel.cs
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemShopPanel : MonoBehaviour
{
	[Header("Data")]
	public RoomItemShopCatalog database;          // 既存：ショップに並べるアイテム一覧
	[SerializeField] ShopCategoryCatalog categories; // ★追加：全カテゴリ一覧（RoomItemCategoryDef[]）

	[Header("UI")]
	public GameObject panelRoot;                  // 既存
	public Transform listParent;                  // 既存：ScrollView/Content
	public RoomItemShopView itemViewPrefab;       // 既存
	public Button closeButton;                    // 既存
	[SerializeField] RoomStateSO roomState;       // 既存
	public bool startHidden = true;               // 既存

	[Header("Tabs (New)")]
	[SerializeField] Transform tabsRoot;          // ★追加：タブの親（HorizontalLayoutGroup 推奨）
	[SerializeField] GameObject tabButtonPrefab;  // ★追加：ShopCategoryTabButton が付いたPrefab
	[SerializeField] bool showOnlyUsableInRoom = true;

	bool _built;
	bool _dirty;
	RoomItemCategoryDef _selectedCategory;
	readonly List<ShopCategoryTabButton> _tabButtons = new();

	public bool IsOpen => panelRoot && panelRoot.activeSelf;

	void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Close);
		if (panelRoot && startHidden) panelRoot.SetActive(false);
		if (roomState) roomState.OnChanged += HandleRoomChanged;
	}

	void OnDestroy()
	{
		if (roomState) roomState.OnChanged -= HandleRoomChanged;
	}

	void HandleRoomChanged(string _)
	{
		if (IsOpen) RebuildAll();
		else _dirty = true;
	}

	public void Open()
	{
		if (!_built || _dirty)
		{
			RebuildAll();
			_dirty = false;
		}
		if (panelRoot) panelRoot.SetActive(true);

		// 保険：開いた瞬間に全行を最新化
		foreach (Transform t in listParent)
		{
			var v = t.GetComponent<RoomItemShopView>();
			if (v) v.SendMessage("Refresh", CurrencyManager.coins, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void Close()
	{
		if (panelRoot) panelRoot.SetActive(false);
	}

	public void Toggle()
	{
		if (IsOpen) Close();
		else Open();
	}

	// ===== タブ生成/更新 =====
	void BuildTabs()
	{
		if (!tabsRoot || !tabButtonPrefab || categories == null || categories.categories == null) return;

		// 既存タブ掃除
		foreach (Transform c in tabsRoot) Destroy(c.gameObject);
		_tabButtons.Clear();

		// sortOrder → displayName で安定ソート
		var ordered = categories.categories
				.Where(c => c != null)
				.OrderBy(c => c.sortOrder)
				.ThenBy(c => c.displayName)
				.ToList();

		// タブ生成
		foreach (var cat in ordered)
		{
			var go = Instantiate(tabButtonPrefab, tabsRoot);
			var tab = go.GetComponent<ShopCategoryTabButton>();
			bool selected = (cat == _selectedCategory);
			tab.Setup(cat, OnSelectCategory, selected);
			_tabButtons.Add(tab);
		}

		// 初回選択（何もなければ先頭）
		if (_selectedCategory == null && ordered.Count > 0)
		{
			_selectedCategory = ordered[0];
			MarkSelectedTab();
		}
	}

	void OnSelectCategory(RoomItemCategoryDef cat)
	{
		if (_selectedCategory == cat) return;
		_selectedCategory = cat;
		MarkSelectedTab();
		RefreshList(); // カテゴリ変更で再描画
	}

	void MarkSelectedTab()
	{
		foreach (var t in _tabButtons)
			t.SetSelected(t && t.Category == _selectedCategory);
	}

	// ===== リスト構築 =====
	void BuildList()
	{
		_built = true;
		if (!listParent || !itemViewPrefab || database == null) return;

		foreach (Transform t in listParent) Destroy(t.gameObject);

		var roomId = roomState ? roomState.CurrentId : null;

		IEnumerable<RoomItemDef> items = database.items.Where(x => x != null);

		// 1) カテゴリで絞る
		if (_selectedCategory != null)
			items = items.Where(x => x.category == _selectedCategory);

		// 2) シーン（部屋）で絞る（空=全許可）

		var names = items.Select(x => x.displayName); // ← 名前だけ取り出す
		if (showOnlyUsableInRoom && !string.IsNullOrEmpty(roomId))
			items = items.Where(x => x.CanUseIn(roomId));
		names = items.Select(x => x.displayName); // ← 名前だけ取り出す

		// （任意）表示順：価格→名前 等
		// items = items.OrderBy(x => x.price).ThenBy(x => x.displayName);

		foreach (var item in items)
		{
			var view = Instantiate(itemViewPrefab, listParent);
			view.Set(item);
		}
		_dirty = false; // 再生成完了
	}

	void RefreshList()
	{
		// リストだけ作り直す
		_built = true; // 「タブは出来ている」前提で
		BuildList();
	}

	void RebuildAll()
	{
		_built = false;
		BuildTabs();
		BuildList();
	}
}
