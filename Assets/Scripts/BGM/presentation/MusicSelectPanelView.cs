using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicSelectPanelView : MonoBehaviour
{
	[Header("Panel")]
	[SerializeField] private GameObject panelRoot;
	[SerializeField] private Button closeButton;

	[Header("Tabs")]
	[SerializeField] private Transform tabRoot;              // タブの親（Horizontal/Vertical Layout Group 推奨）
	[SerializeField] private GameObject tabButtonPrefab;     // Button + TMP_Text (+ 任意で Image)

	[Header("List")]
	[SerializeField] private Transform contentRoot;          // トラックリストの親（Layout Group）
	[SerializeField] private GameObject itemPrefab;          // Button + TMP_Text

	private IMusicService _service;
	private bool _builtOnce = false;
	private int _activeCatalogIndex = 0;

	private void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Hide);
		// if (panelRoot) panelRoot.SetActive(false);
	}

	/// <summary>サービスを受け取る（UI側から1回呼ぶ）</summary>
	public void InitWithService(IMusicService service)
	{
		_service = service;

		// サービス側の変更イベントに合わせてUI更新
		if (_service != null)
		{
			_service.OnChanged += OnServiceChanged;
			_activeCatalogIndex = _service.CurrentCatalogIndex;
		}
	}

	public void Show()
	{
		if (!_builtOnce)
		{
			BuildTabs();
			BuildList();
			_builtOnce = true;
		}
		if (panelRoot) panelRoot.SetActive(true);
	}

	public void Hide()
	{
		if (panelRoot) panelRoot.SetActive(false);
	}

	private void OnDestroy()
	{
		if (_service != null)
			_service.OnChanged -= OnServiceChanged;
	}

	private void OnServiceChanged(MusicSnapshot snap)
	{
		// カタログが変わったらUIを更新（開いている時だけで十分）
		if (panelRoot && panelRoot.activeSelf)
		{
			if (_activeCatalogIndex != snap.CurrentCatalogIndex)
			{
				_activeCatalogIndex = snap.CurrentCatalogIndex;
				BuildTabs();
				BuildList();
			}
			else
			{
				// 同一カタログ内の選曲変化時はタブはそのまま、リストは再構築不要
				// 必要なら選択中アイテムのハイライトを入れる場合にここで処理
			}
		}
	}

	// ============== UI Build ==============

	private void BuildTabs()
	{
		if (tabRoot == null || tabButtonPrefab == null || _service == null) return;

		// 既存クリア
		for (int i = tabRoot.childCount - 1; i >= 0; i--)
			Destroy(tabRoot.GetChild(i).gameObject);

		var set = _service.CatalogSet;
		if (set == null || set.catalogs == null || set.catalogs.Count == 0)
		{
			// セット未設定時は何も出さない（または1つのダミータブを出したい場合はここで生成）
			return;
		}

		// タブ生成
		for (int i = 0; i < set.catalogs.Count; i++)
		{
			var item = set.catalogs[i];
			var go = Instantiate(tabButtonPrefab, tabRoot);

			// ラベル
			var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label) label.text = string.IsNullOrEmpty(item.displayName) ? $"Catalog {i}" : item.displayName;

			// 任意：アイコン
			var img = go.GetComponentInChildren<Image>(true);
			if (img && item.tabIcon) img.sprite = item.tabIcon;

			// クリック
			var btn = go.GetComponent<Button>();
			int idx = i;
			if (btn)
			{
				btn.onClick.AddListener(() =>
				{
					if (_activeCatalogIndex == idx) return;
					_activeCatalogIndex = idx;
					BuildTabs(); // ハイライト更新
					BuildList(); // リスト更新
				});

				// 簡易ハイライト：選択中は押せないように
				btn.interactable = (i != _activeCatalogIndex);
			}
		}
	}

	private void BuildList()
	{
		if (contentRoot == null || itemPrefab == null || _service == null) return;

		// 既存クリア
		for (int i = contentRoot.childCount - 1; i >= 0; i--)
			Destroy(contentRoot.GetChild(i).gameObject);

		var set = _service.CatalogSet;
		if (set == null || set.catalogs == null || set.catalogs.Count == 0) return;

		_activeCatalogIndex = Mathf.Clamp(_activeCatalogIndex, 0, set.catalogs.Count - 1);
		var cat = set.catalogs[_activeCatalogIndex]?.catalog;
		if (cat == null || cat.tracks == null || cat.tracks.Length == 0) return;

		for (int i = 0; i < cat.tracks.Length; i++)
		{
			var entry = cat.tracks[i];
			var go = Instantiate(itemPrefab, contentRoot);

			var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label) label.text = string.IsNullOrEmpty(entry.displayName) ? $"Track {i}" : entry.displayName;

			var btn = go.GetComponent<Button>();
			int trackIndex = i;
			if (btn)
			{
				btn.onClick.AddListener(() =>
				{
					_service.SelectTrack(_activeCatalogIndex, trackIndex, autoPlay: true);
					Hide();
				});
			}
		}

		// レイアウト反映（必要な時だけ）
		Canvas.ForceUpdateCanvases();
		var rt = contentRoot as RectTransform;
		if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
	}
}
