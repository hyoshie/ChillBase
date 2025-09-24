using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicSelectPanelController : MonoBehaviour
{
	[SerializeField] private GameObject panelRoot;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform contentRoot;
	[SerializeField] private GameObject itemPrefab;

	[Header("Catalog")]
	[SerializeField] private TrackCatalog catalog; // 直接参照

	private MusicController _controller;

	private void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Hide);
	}

	public void Init(MusicController controller)
	{
		_controller = controller;
		BuildList();
		if (panelRoot) panelRoot.SetActive(false);
	}

	public void Show() => panelRoot?.SetActive(true);
	public void Hide() => panelRoot?.SetActive(false);

	private void BuildList()
	{
		if (catalog == null || catalog.tracks.Length == 0) return;

		for (int i = 0; i < catalog.tracks.Length; i++)
		{
			var entry = catalog.tracks[i];
			var go = Instantiate(itemPrefab, contentRoot);

			var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label) label.text = string.IsNullOrEmpty(entry.displayName) ? $"Track {i}" : entry.displayName;

			var btn = go.GetComponent<Button>();
			int index = i;
			if (btn)
			{
				btn.onClick.AddListener(() =>
				{
					_controller.SelectTrack(index, true);
					Hide();
				});
			}
		}
	}
}
