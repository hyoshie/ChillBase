using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MusicController : MonoBehaviour
{
	[Header("Audio")]
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private List<AudioClip> audioClips = new();
	// MusicController.cs のクラス内に追加
	public IReadOnlyList<AudioClip> Clips => audioClips;
	public int CurrentIndex => currentIndex;


	[Header("UI - Controls")]
	[SerializeField] private Button prevButton;
	[SerializeField] private Button playPauseButton;
	[SerializeField] private Button nextButton;

	[Header("UI - PlayMode")]
	[SerializeField] private Button playModeButton;   // モード切替用ボタン
	[SerializeField] private Image playModeIcon;      // 現在のモード表示
	[SerializeField] private Sprite sequentialSprite; // 順番再生アイコン
	[SerializeField] private Sprite repeatOneSprite;  // 一曲リピートアイコン

	[Header("UI - Display")]
	[SerializeField] private TextMeshProUGUI titleText;
	[SerializeField] private Image playPauseIcon;
	[SerializeField] private Sprite playSprite;
	[SerializeField] private Sprite pauseSprite;

	[SerializeField] private Button openSelectionPanelButton;
	[SerializeField] private MusicSelectPanelController selectionPanel;

	// ★ デバッグ
	[Header("UI - Debug")]
	[SerializeField] private Button jumpLast5SecButton; // 残り5秒へ

	[Header("Options")]
	[SerializeField] private bool autoPlayOnStart = false;
	[SerializeField] private bool loopPlaylist = true; // Sequential時のみ利用

	private int currentIndex = -1;
	private bool isPlaying = false;
	private PlayMode playMode;

	private void Awake()
	{
		if (prevButton) prevButton.onClick.AddListener(Prev);
		if (playPauseButton) playPauseButton.onClick.AddListener(TogglePlay);
		if (nextButton) nextButton.onClick.AddListener(Next);
		if (playModeButton) playModeButton.onClick.AddListener(CyclePlayMode);
		if (jumpLast5SecButton) jumpLast5SecButton.onClick.AddListener(JumpToLast5Seconds);
		if (openSelectionPanelButton) openSelectionPanelButton.onClick.AddListener(() =>
{
	if (selectionPanel) selectionPanel.Show();
});

		// 保存されたモードをロード
		playMode = PlayModeStore.Load();
	}

	private void Start()
	{
		if (!audioSource) return;
		audioSource.loop = false;
		audioSource.playOnAwake = false;

		if (audioClips.Count > 0) SelectTrack(0, autoPlayOnStart);
		if (selectionPanel) selectionPanel.Init(this);
		UpdateUI();
	}

	private void Update()
	{
		// 曲が終わったらモードごとの挙動
		if (isPlaying && audioSource && audioSource.clip &&
				!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.05f)
		{
			HandleTrackFinished();
		}
	}

	// --------------------
	// 再生モード切替
	// --------------------
	private void CyclePlayMode()
	{
		var values = System.Enum.GetValues(typeof(PlayMode)).Cast<PlayMode>().ToArray();
		int idx = System.Array.IndexOf(values, playMode);
		int next = (idx + 1) % values.Length;
		playMode = values[next];

		PlayModeStore.Save(playMode);
		UpdateUI_PlayMode();
	}

	private void HandleTrackFinished()
	{
		switch (playMode)
		{
			case PlayMode.RepeatOne:
				if (currentIndex >= 0) SelectTrack(currentIndex, true);
				else { isPlaying = false; UpdateUI(); }
				break;

			case PlayMode.Sequential:
			default:
				if (loopPlaylist) Next(true);
				else { isPlaying = false; UpdateUI(); }
				break;
		}
	}

	// --------------------
	// 再生コントロール
	// --------------------
	public void TogglePlay()
	{
		if (!audioSource || !audioSource.clip) return;

		if (isPlaying) { audioSource.Pause(); isPlaying = false; }
		else { audioSource.Play(); isPlaying = true; }

		UpdateUI();
	}

	public void Next() => Next(false);

	public void Next(bool autoPlay)
	{
		if (audioClips.Count == 0) return;
		int next = (currentIndex + 1) % audioClips.Count;
		SelectTrack(next, autoPlay || isPlaying);
	}

	public void Prev()
	{
		if (audioClips.Count == 0) return;
		int prev = (currentIndex - 1 + audioClips.Count) % audioClips.Count;
		SelectTrack(prev, isPlaying);
	}

	public void SelectTrack(int index, bool autoPlay = false)
	{
		if (!audioSource || index < 0 || index >= audioClips.Count) return;

		currentIndex = index;
		audioSource.clip = audioClips[index];
		audioSource.time = 0f;

		if (autoPlay) { audioSource.Play(); isPlaying = true; }
		else { audioSource.Pause(); isPlaying = false; }

		UpdateUI();
	}

	// --------------------
	// デバッグ: 残り5秒へジャンプ
	// --------------------
	// 残り5秒へ（短い曲は残り0.1秒へ）ジャンプ
	private void JumpToLast5Seconds()
	{
		if (!audioSource || !audioSource.clip) return;

		var clip = audioSource.clip;
		bool wasPlaying = isPlaying || audioSource.isPlaying;

		// 5秒より短い曲は、終端の手前0.1秒に置く
		float backSeconds = (clip.length > 5f) ? 5f : 0.1f;

		int offsetSamples = Mathf.CeilToInt(clip.frequency * backSeconds);
		int targetSamples = Mathf.Clamp(clip.samples - offsetSamples, 0, clip.samples - 1);

		// ※ Play() で先頭に戻るのを避けるため Stop → timeSamples セット
		audioSource.Stop();
		audioSource.timeSamples = targetSamples;

		if (wasPlaying)
		{
			audioSource.Play();
			isPlaying = true; // 自前フラグも同期
		}

		UpdateUI();
	}


	// --------------------
	// UI更新
	// --------------------
	private void UpdateUI()
	{
		if (titleText)
		{
			string name = (currentIndex >= 0 && currentIndex < audioClips.Count && audioClips[currentIndex])
					? audioClips[currentIndex].name : "-";
			titleText.text = name;
		}

		if (playPauseIcon && playSprite && pauseSprite)
			playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;

		bool hasClip = audioClips.Count > 0;
		if (prevButton) prevButton.interactable = hasClip;
		if (nextButton) nextButton.interactable = hasClip;
		if (playPauseButton) playPauseButton.interactable = hasClip;

		UpdateUI_PlayMode();
		if (playModeButton) playModeButton.interactable = true;
	}

	private void UpdateUI_PlayMode()
	{
		if (!playModeIcon) return;

		switch (playMode)
		{
			case PlayMode.RepeatOne:
				if (repeatOneSprite) playModeIcon.sprite = repeatOneSprite;
				break;

			case PlayMode.Sequential:
			default:
				if (sequentialSprite) playModeIcon.sprite = sequentialSprite;
				break;
		}
	}
}
