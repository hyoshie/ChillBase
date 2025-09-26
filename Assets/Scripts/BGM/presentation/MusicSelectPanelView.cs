using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicSelectPanelView : MonoBehaviour
{
	[SerializeField] private GameObject panelRoot;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform contentRoot;
	[SerializeField] private GameObject itemPrefab;

	// ▼ 変更：Inspectorからは設定しない（非Serialize）
	private TrackCatalog _catalog;

	private IMusicService _service;
	private bool _built = false;

	private void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Hide);
		// if (panelRoot) panelRoot.SetActive(false);
	}

	/// <summary>サービスを受け取ってリストを構築（UIは呼ぶだけ）</summary>
	public void InitWithService(IMusicService service)
	{
		_service = service;

		// ▼ 追加：サービス（SO）からカタログを受け取る
		_catalog = service.Catalog;

		// 必要になった時にビルド（Show時の遅延ビルドでOK）
		// BuildList();
	}

	public void Show()
	{
		if (!_built)
		{
			BuildList();
		}

		if (panelRoot) panelRoot.SetActive(true);
	}

	public void Hide()
	{
		if (panelRoot) panelRoot.SetActive(false);
	}

	private void BuildList()
	{
		if (contentRoot == null || itemPrefab == null)
		{
			_built = true;
			return;
		}

		// 既存アイテム削除
		for (int i = contentRoot.childCount - 1; i >= 0; i--)
			Destroy(contentRoot.GetChild(i).gameObject);

		// ▼ 参照元は _catalog に一本化
		if (_catalog == null || _catalog.tracks == null || _catalog.tracks.Length == 0)
		{
			_built = true;
			return;
		}

		for (int i = 0; i < _catalog.tracks.Length; i++)
		{
			var entry = _catalog.tracks[i];
			var go = Instantiate(itemPrefab, contentRoot);

			var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label) label.text = string.IsNullOrEmpty(entry.displayName) ? $"Track {i}" : entry.displayName;

			var btn = go.GetComponent<Button>();
			int index = i;
			if (btn)
			{
				btn.onClick.AddListener(() =>
				{
					_service?.SelectTrack(index, autoPlay: true);
					Hide();
				});
			}
		}

		_built = true;
	}

	// /// <summary>（任意）外部からカタログ差し替え時に再ビルドしたい場合</summary>
	// public void SetCatalog(TrackCatalog newCatalog, bool rebuild = true)
	// {
	// 	_catalog = newCatalog;
	// 	_built = false;
	// 	if (rebuild && panelRoot && panelRoot.activeSelf)
	// 	{
	// 		BuildList();
	// 		Canvas.ForceUpdateCanvases();
	// 		var rt = contentRoot as RectTransform;
	// 		if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
	// 	}
	// }
}
