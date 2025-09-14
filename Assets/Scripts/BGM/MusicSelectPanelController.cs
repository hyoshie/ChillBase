using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;


public class MusicSelectPanelController : MonoBehaviour
{
	[SerializeField] private GameObject panelRoot;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform contentRoot;
	[SerializeField] private GameObject itemPrefab;

	private MusicController _controller;

	private void Awake()
	{
		if (closeButton) closeButton.onClick.AddListener(Hide);
		// if (panelRoot) panelRoot.SetActive(false);
	}

	// 初期化用：ゲーム開始時に1回だけ呼ぶ
	public void Init(MusicController controller)
	{
		_controller = controller;
		BuildList();
		if (panelRoot) panelRoot.SetActive(false); // Initの最後で非アクティブ化
	}

	public void Show()
	{
		if (panelRoot) panelRoot.SetActive(true);
	}

	public void Hide()
	{
		if (panelRoot) panelRoot.SetActive(false);
	}

	private void BuildList()
	{
		if (_controller == null || _controller.Clips == null || _controller.Clips.Count == 0) return;

		for (int i = 0; i < _controller.Clips.Count; i++)
		{
			var clip = _controller.Clips[i];
			var go = Instantiate(itemPrefab, contentRoot);

			var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label) label.text = clip ? clip.name : $"Track {i}";

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
