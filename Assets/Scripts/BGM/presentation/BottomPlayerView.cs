using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BottomPlayerView : MonoBehaviour
{
	[SerializeField] private MusicServiceSO service;

	[Header("UI - Controls")]
	[SerializeField] private Button prevButton;
	[SerializeField] private Button playPauseButton;
	[SerializeField] private Button nextButton;

	[Header("UI - PlayMode")]
	[SerializeField] private Button playModeButton;
	[SerializeField] private Image playModeIcon;
	[SerializeField] private Sprite sequentialSprite;
	[SerializeField] private Sprite repeatOneSprite;

	[Header("UI - Display")]
	[SerializeField] private TextMeshProUGUI titleText;
	[SerializeField] private Image playPauseIcon;
	[SerializeField] private Sprite playSprite;
	[SerializeField] private Sprite pauseSprite;

	[Header("UI - Other")]
	[SerializeField] private Button openSelectionPanelButton;
	[SerializeField] private MusicSelectPanelView selectionPanel;
	[SerializeField] private Button jumpLast5SecButton;

	private void Awake()
	{
		if (service == null)
		{
			Debug.LogError("[BottomPlayerView] MusicServiceSO not assigned.");
			return;
		}

		prevButton?.onClick.AddListener(() => service.Prev());
		playPauseButton?.onClick.AddListener(() => service.TogglePlay());
		nextButton?.onClick.AddListener(() => service.Next());
		playModeButton?.onClick.AddListener(() => service.CyclePlayMode());
		jumpLast5SecButton?.onClick.AddListener(() => service.JumpToLast5Seconds());
		openSelectionPanelButton?.onClick.AddListener(() => selectionPanel?.Show());

		service.OnChanged += OnServiceChanged;
		// selectionPanel.InitWithService(service);
	}

	private void Start()
	{
		selectionPanel.InitWithService(service);
	}

	private void OnDestroy()
	{
		if (service != null) service.OnChanged -= OnServiceChanged;
	}

	private void OnServiceChanged(MusicSnapshot s)
	{
		if (titleText) titleText.text = s.Title ?? string.Empty;

		if (playPauseIcon && playSprite && pauseSprite)
			playPauseIcon.sprite = s.IsPlaying ? pauseSprite : playSprite;

		bool has = s.HasAnyTrack;
		if (prevButton) prevButton.interactable = has;
		if (nextButton) nextButton.interactable = has;
		if (playPauseButton) playPauseButton.interactable = has;

		if (playModeIcon)
			playModeIcon.sprite = (s.PlayMode == PlayMode.RepeatOne) ? repeatOneSprite : sequentialSprite;

		if (playModeButton) playModeButton.interactable = true;
	}
}
